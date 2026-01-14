# 🎯 KPI COMMISSION SYSTEM - HƯỚNG DẪN SỬ DỤNG

## 📊 Tổng quan hệ thống

Hệ thống KPI Commission (đơn giản) gồm2 phần:
1) **Dashboard (estimate)**: Sale xem doanh số/thưởng dự kiến realtime (không lưu DB).
2) **Chốt KPI tháng (persist)**: Admin chạy `calculateMonthlyKpi` để **tính & lưu** hoa hồng KPI theo tháng.

> Lưu ý quan trọng theo code hiện tại:
> - Khi `Order` chuyển `Status = Paid` **KHÔNG tự động tạo record hoa hồng**.
> - Hoa hồng (base/bonus) chỉ **được tính & lưu** khi Admin chạy `calculateMonthlyKpi`.

---

## 📌 Dữ liệu được dùng để tính doanh số

Trong code hiện tại (`KpiService`), doanh số tháng của Sale được tính như sau:
- Lọc **Orders của Sale** theo **CreatedAt trong tháng** (từ ngày1 đến hết tháng)
- Chỉ lấy các đơn có `Status == Paid`
- `TotalRevenue = Sum(order.TotalPrice)`

> Nghĩa là: hệ thống đang coi doanh số thuộc về **tháng tạo đơn** (`CreatedAt`), không phải theo thời điểm thanh toán (`PaidAt`).
> Đây là lựa chọn đơn giản để phù hợp yêu cầu điểm thấp.

---

## 📈 KPI Tiers (Seed Data mặc định)

> Simplified KPI: Sale có `TargetRevenue` (mục tiêu tháng). Khi **đạt target** mới có thưởng KPI.
> Tier dựa trên **% hoàn thành target**.

| Tier | Điều kiện (Progress so với target) | Bonus % |
|------|-----------------------------------|---------|
| Bronze | ≥100% | +2% |
| Silver | ≥120% | +5% |
| Gold | ≥150% | +8% |
| Platinum | ≥200% | +12% |

**Mapping trong DB:** do giữ schema đơn giản, field `KpiTier.MinRevenue` đang được dùng như:
- `MinRevenue` = **MinAchievedPercent** (vd:100/120/150/200)

---

## 💰 Cách tính hoa hồng (đúng theo code)

###1) Base Commission (estimate & persist)
```
BaseCommission = TotalRevenue *10%
```

###2) KPI Bonus Commission (chỉ khi có target & đạt target)
- Nếu **không có target tháng** → `BonusCommission =0`
- Nếu **TotalRevenue < TargetRevenue** → `BonusCommission =0`
- Nếu **TotalRevenue ≥ TargetRevenue**:
 - `AchievedPct = floor(TotalRevenue *100 / TargetRevenue)`
 - Chọn tier có `MinAchievedPercent <= AchievedPct` lớn nhất
 - `BonusCommission = TotalRevenue * BonusPercent`

###3) Total
```
TotalCommission = BaseCommission + BonusCommission
```

---

## 🧩 Flow thực tế

### Case A — Sale tạo đơn & đơn được chuyển sang Paid
- Thao tác: cập nhật `Order.Status = Paid` (qua `updateOrder` / service Order)
- Kết quả:
 - Chỉ lưu trạng thái Paid trong bảng `Orders`
 - **Không tính và không lưu hoa hồng** tại thời điểm này

### Case B — Sale xem dashboard KPI (realtime estimate)
API: `kpiDashboard(saleId, year?, month?)`
- Hệ thống tính toán **tại thời điểm query**:
 - `ActualRevenue = sum(TotalPrice của các order Paid trong tháng)`
 - `EstimatedBaseCommission = ActualRevenue *10%`
 - Nếu có target và đạt target → tính tier & `EstimatedBonusCommission`
- **Không lưu DB** (chỉ trả về response)

### Case C — Admin chốt KPI tháng (tính & lưu)
API: `calculateMonthlyKpi(input: { year, month, saleId? })`
- Hệ thống:
 - Tính `TotalRevenue/Base/Bonus/Total` cho từng Sale theo tháng
 - **Upsert** vào bảng `KpiCommissions` theo key `(SaleId, Year, Month)`
 - Nếu tồn tại target trong `SaleKpiTargets` thì update snapshot:
 - `ActualRevenue`, `BonusAmount`, `KpiTierId`, `CalculatedAt`

---

## 🗃️ Dữ liệu được lưu ở đâu?

###1) Bảng `Orders`
- Lưu trạng thái order (`Created`, `Paid`, ...)
- KPI service đọc từ đây để tính doanh số tháng

###2) Bảng `SaleKpiTargets`
- Lưu target theo tháng do Admin/Moderator set
- Khi chạy `calculateMonthlyKpi`, hệ thống update:
 - `ActualRevenue`, `BonusAmount`, `KpiTierId`, `CalculatedAt`

###3) Bảng `KpiCommissions`
- Lưu kết quả chốt KPI theo tháng:
 - `BaseCommission`, `BonusCommission`, `TotalCommission`
 - `TotalRevenue`, `TotalOrders`, `KpiTierId`, `CalculatedAt`

---

## 📝 Notes

1) **Hiện tại hệ thống KHÔNG tự động persist hoa hồng khi order Paid**.
 - Nếu muốn “tự động”, cần bổ sung logic nếu status đổi sang Paid thì trigger tính toán.

2) **Doanh số đang lọc theo `Orders.CreatedAt`** (tháng tạo đơn).
 - Nếu muốn sát thực tế hơn: lọc theo `Payments.PaidAt`.

3) `calculateMonthlyKpi` có thể chạy nhiều lần (upsert) để chốt lại số liệu.

---

## 🪟 WinUI3 Frontend gợi ý (sử dụng endpoints – tối giản màn hình)
Gợi ý 2 màn hình chính (tùy role mà hiện/ẩn tab):

Tạo 1 navigation menu item "KPI Commission" với2 sub-item:
1) **KPI Dashboard** (dùng cho Sale + Admin/Moderator)
2) **KPI Admin** (chỉ Admin/Moderator; trong1 page có3 tab: Targets / Calculate / Tiers)

---

###1) Màn hình `KPI Dashboard`

**Chức năng:** xem KPI realtime + xem lịch sử (có thể gộp cùng màn hình bằng tab/expander).

**UI gợi ý (tối giản):**
- Combobox chọn `Year/Month` (default: current)
- Cards: Target, Actual, Progress, Tier, Base/Bonus/Total (estimated)
- Tab "History" (DataGrid danh sách tháng đã chốt)

**API gọi & thời điểm gọi:**
- Khi vào màn hình (OnNavigatedTo):
 - `kpiDashboard(saleId: currentUser.id, year: now.Year, month: now.Month)`
 - `kpiCommissions(saleId: currentUser.id, year: now.Year, month: null, page:1, pageSize:12)` (đổ tab History)
- Khi đổi Year/Month:
 - gọi lại `kpiDashboard(saleId, year, month)`
- Tuỳ chọn auto refresh (30s):
 - gọi lại `kpiDashboard(saleId, year, month)`

> Role Admin/Moderator: cho phép chọn saleId bằng ComboBox, rồi gọi `kpiDashboard(saleIdSelected, year, month)`.

---

###2) Màn hình `KPI Admin` (Admin/Moderator)

Gộp 3 chức năng vào 1 page bằng TabView:

#### Tab A — `Targets`
- Filter Year/Month
- DataGrid list target của sale
- Edit target tại từng dòng

**API:**
- OnTabOpened / OnNavigatedTo:
 - `saleKpiTargets(saleId: null, year, month, page:1, pageSize:100)`
- Khi bấm Save target1 sale:
 - `setMonthlyTarget(input: { saleId, year, month, targetRevenue })`
 - Sau đó refresh: `saleKpiTargets(...)`

#### Tab B — `Calculate`
- Pick Year/Month
- Button "Calculate" (all sales hoặc 1 sale)

**API:**
- Click Calculate:
 - `calculateMonthlyKpi(input: { year, month, saleId? })`
 - Sau success refresh report:
 - `kpiCommissions(saleId: null, year, month, page:1, pageSize:100)`

#### Tab C — `Tiers` (Admin only)
- DataGrid tier list
- CRUD tiers

**API:**
- OnTabOpened:
 - `kpiTiers(page:1, pageSize:100)`
- Create/Update/Delete:
 - `createKpiTier(input)` / `updateKpiTier(kpiTierId, input)` / `deleteKpiTier(kpiTierId)`
 - Sau đó refresh lại `kpiTiers(...)`

---

### Gợi ý “when to call” (best practice ngắn)
- Tiers: cache trong memory (ít thay đổi), chỉ refresh khi CRUD.
- Dashboard: có thể auto refresh30s.
- Reports/Targets: refresh manual theo Year/Month hoặc sau khi mutation thành công.
