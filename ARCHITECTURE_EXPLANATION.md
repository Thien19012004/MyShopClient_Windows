# Selection Persistence Architecture

## Problem: Search clears data

User scenario:
1. Open SELECT PRODUCTS dialog
2. Tick: Acer Predator (#40), Acer Swift (#39)
3. Search "iPhone" 
4. ? LEFT panel shows only iPhone products (Acer products disappear)
5. ? Acer checkboxes can't be restored (products not in list)

---

## Solution: Two-Level Storage

### Level 1: ProductSelectorVm.Products (Temporary)
- **What**: Current page of products displayed in LEFT panel
- **Behavior**: Gets **cleared and reloaded** on every search/page change
- **Lifetime**: Only valid while user views the dialog

```
Products.Clear()  ? happens on search
?
Products.Load new search results
```

### Level 2: AddVm.SelectedProducts / EditVm.SelectedProducts (Persistent Cache)
- **What**: User's selected items (permanent storage in this session)
- **Behavior**: NEVER cleared, only Add/Remove operations
- **Lifetime**: Survives search, pagination, everything until user clicks Confirm

```
SelectedProducts = [
  ProductItemDto { ProductId: 40, Name: "Acer Predator" },
  ProductItemDto { ProductId: 39, Name: "Acer Swift" }
]
?
Persists across search/reload!
```

---

## Data Flow: Real-Time Selection

### When User Ticks Checkbox:

```
ProductCheckBox_Tapped(sender=checkbox, product=Acer)
  ?
product.IsSelected = true
  ?
Add to AddVm.SelectedProducts  ? ? PERSISTS HERE
  ?
RIGHT panel updates immediately (Observable Collection)
```

### When User Searches "iPhone":

```
Search text changes
  ?
ProductSelectorVm.LoadProductsAsync()
  ?
Products.Clear()  ? removes old (Acer, Swift)
  ?
Products.Add(iPhone products)  ? new list on LEFT
  ?
OnProductsLoaded callback triggers
  ?
ApplySelectedStatesToProducts()
  ?
Compare new Products with AddVm.SelectedProducts
  ?
product.IsSelected = SelectedProducts.Contains(product)
  ?
If product is in SelectedProducts ? checkbox marked ?
If product NOT in SelectedProducts ? checkbox unmarked
```

### Key: Products not in current list

```
LEFT Panel (ProductSelectorVm.Products):
?? iPhone 13 128GB (IsSelected: false) ? can't restore Acer here
?? iPhone 14 Pro (IsSelected: false)
?? (no Acer products because they don't match "iPhone" search)

BUT AddVm.SelectedProducts still has:
?? Acer Predator (#40)
?? Acer Swift (#39)
?? (all persisted in cache)

When user clears search:
  ProductSelectorVm.LoadProductsAsync() with empty SearchText
  ?
  Products.Clear()
  ?
  Products.Load([Acer Predator, Acer Swift, iPhone 13, ...])
?
  OnProductsLoaded callback
  ?
  Acer Predator.IsSelected = true  ? restored! ?
  Acer Swift.IsSelected = true     ? restored! ?
  iPhone 13.IsSelected = false     ? not in SelectedProducts
```

---

## Why This Works

1. **SelectedProducts is source of truth** (not LEFT panel)
2. **Products is just display** (filtered by search)
3. **Callback syncs** Products ? SelectedProducts on every load
4. **Selections persist** because cache never clears

## Visual Summary

```
???????????????????????????????????
?  User Action: Search "iPhone"   ?
???????????????????????????????????
     ?
???????????????????????????????????????????
? ProductSelectorVm.LoadProductsAsync()   ?
? - Products.Clear()  ? removes old       ?
? - Load new filtered results      ?
???????????????????????????????????????????
        ?
????????????????????????????????????????????????
? OnProductsLoaded Callback            ?
? AddVm.ApplySelectedStatesToProducts(Products)?
?  ?
? foreach (product in Products)     ?
?   product.IsSelected =              ?
?     SelectedProducts.Contains(product)       ?
????????????????????????????????????????????????
             ?
????????????????????????????????
? Result:       ?
? ? iPhone checkboxes: false  ?
? (not in SelectedProducts)    ?
?                   ?
? Acer products: not visible   ?
? (not in current search)      ?
?      ?
? RIGHT panel: still shows     ?
? Acer Predator ?      ?
? Acer Swift ?  ?
? (from SelectedProducts cache)?
????????????????????????????????
```

---

## When User Clears Search or Navigates Back

```
Search text cleared  OR  User goes back to "all products"
  ?
LoadProductsAsync()
  ?
Products loads ALL products (Acer + iPhone + ...)
  ?
OnProductsLoaded callback
  ?
Acer Predator.IsSelected = true   ? ? restored from cache!
Acer Swift.IsSelected = true      ? ? restored from cache!
iPhone 13.IsSelected = false      ? ? correct (not selected)
```

---

## Summary

? **Cache (SelectedProducts)** = permanent, never cleared
? **Display (Products)** = temporary, cleared on search
? **Callback** = syncs them together on every load
? **RIGHT panel** = always shows cache (true state)
? **LEFT panel** = filtered view + restored states from cache
