# Step 02 — Shell navigation, commands and parameters

- **Lab:** 2 (`docks/Лабораторна_робота_2_Команди_Навігація_MAUI.pdf`)
- **Branch:** `lab2` (on top of `lab1`)
- **PR:** _to be added_

## 1. Before

After Lab 1 the app had three pages (login, product list, add-product form),
a minimal `INavigationService.GoToAsync(string route)`, `Routes`, and one
registered route (`productform`). The list was a flat read-only list that
reloaded itself every time it appeared. There was no product page, no way to
see or change characteristic values, no sorting, and no page for recording
stock movements (`IStockService.RecordAsync` existed but had no UI).

## 2. What the lab asks for

Translated from the assignment (the lab uses `Student` / `StudentDetailPage`):

**Part 1 (in class)**

1. Register the detail page's route in `AppShell` with `Routing.RegisterRoute`.
2. Create the detail page and its ViewModel; the page shows the selected
   item's data through bindings; the ViewModel is the `BindingContext` (DI).
3. Add an `OpenDetailsCommand` (a command with a parameter) to the list's
   ViewModel. It calls `GoToAsync` with a dictionary holding the selected
   object. In XAML it is bound to a "Details" button with
   `CommandParameter="{Binding .}"` or to `SelectionChangedCommand`.
   Navigation must not start in code-behind.
4. Receive the object in the detail ViewModel with `IQueryAttributable`
   (`ApplyQueryAttributes`) and fill the properties.

**Part 2 (independent)**

1. `GoBackCommand` on the detail ViewModel: `GoToAsync("..")`, bound to a Back button.
2. Edit a value on the detail page; `SaveCommand` sends the updated object back
   to the list either through `GoToAsync("..", parameters)` + `IQueryAttributable`
   on the list ViewModel, or through `WeakReferenceMessenger`. The list shows the
   change at once, without restarting the app.
3. Written answers: why navigation from code-behind breaks MVVM and how
   `ICommand` fixes it; query string vs `IQueryAttributable`.

## 3. Our interpretation

The warehouse has no "student average score", so the detail page edits the
**characteristic values** of a product, and a new page records **In/Out
operations**. Extra work the lab does not ask for, but the plan does: the
characteristics page, sorting, role-based visibility.

| Lab task | In StockRoom | File |
|----------|--------------|------|
| Register routes | `Routing.RegisterRoute` for `productform`, `productdetail`, `characteristics`, `operation`; route names are constants | `AppShell.xaml.cs`, `Services/Routes.cs` |
| Navigate only from the ViewModel | Every ViewModel navigates through `INavigationService`; only `ShellNavigationService` calls `Shell.Current.GoToAsync` | `Services/INavigationService.cs`, `Services/ShellNavigationService.cs` |
| Detail page + ViewModel (DI) | `ProductDetailPage` gets `ProductDetailViewModel` in its constructor; both registered as transient | `Views/ProductDetailPage.xaml(.cs)`, `ViewModels/ProductDetailViewModel.cs`, `MauiProgram.cs`, `ServiceCollectionExtensions.cs` |
| `OpenDetailsCommand` with `CommandParameter="{Binding .}"` | `OpenDetailsCommand` (`[RelayCommand]` taking a `ProductSummary`) bound to a `TapGestureRecognizer` on each row, `CommandParameter="{Binding .}"` | `ViewModels/ProductListViewModel.cs`, `Views/ProductsPage.xaml` |
| Pass an object in a dictionary | `GoToAsync("productdetail", { "SelectedProduct": summary })` | `Services/NavigationKeys.cs` |
| `IQueryAttributable` on the detail ViewModel | `ProductDetailViewModel.ApplyQueryAttributes` reads `SelectedProduct`; also used by `OperationViewModel` | `ViewModels/ProductDetailViewModel.cs`, `ViewModels/OperationViewModel.cs` |
| `GoBackCommand` (`".."`) | `GoBackCommand` on detail, characteristics and operation ViewModels; bound to the Back button, to `Shell.BackButtonBehavior` (toolbar arrow) and to the hardware back button | `ViewModels/*`, `Views/*Page.xaml(.cs)` |
| Edit + `SaveCommand` returning data | Manager edits characteristic values; `SaveCommand` saves through `ICharacteristicService`, then `GoToAsync("..", { "UpdatedProduct": summary })` | `ViewModels/ProductDetailViewModel.cs`, `Services/CharacteristicService.cs` |
| List updates without reload | `ProductListViewModel : IQueryAttributable` replaces (or adds) the row with the same `Id`; the list loads once per signed-in user (`EnsureLoadedCommand`) | `ViewModels/ProductListViewModel.cs` |
| Written explanation | Section 7 below, answers to the control questions | this file |
| (plan) Characteristics page | List and add a characteristic (unique name, Text/Number), Manager only | `Views/CharacteristicsPage.xaml`, `ViewModels/CharacteristicsViewModel.cs` |
| (plan) Sort by any characteristic | Picker of `SortOption`s + Descending switch; logic in the pure `ProductSorter` | `Services/ProductSorter.cs`, `Services/SortOption.cs` |
| (plan) Role-based visibility | Manager-only buttons and the editable entries are hidden or read-only for others, using `RolePermissions` | `ViewModels/*`, `Services/RolePermissions.cs` |
| (plan) In/Out operation page | Direction (Driver: In only), units (Stepper, at least 1), counterparty, reason; `IStockService.RecordAsync`; returns to the detail page, which reloads | `Views/OperationPage.xaml`, `ViewModels/OperationViewModel.cs` |

## 4. Decisions

| Decision | Alternatives considered | Why |
|----------|-------------------------|-----|
| `INavigationService.GoToAsync(route, IDictionary<string, object>)` next to the plain overload | A generic `NavigateTo<TViewModel>(args)` | Same shape as Shell, trivial to fake: the fake records route and parameters |
| Parameters are objects in a dictionary + `IQueryAttributable` | `[QueryProperty]` with ids in the query string | The row is a rich record; no serialisation, and the detail page shows it instantly before loading the rest. The query-string approach is described in the answer to question 3 |
| Return data with `GoToAsync("..", parameters)` handled by the list's `IQueryAttributable` | `WeakReferenceMessenger` | The page that returns data is always opened by the page that receives it, so Shell already connects them; the dependency is visible in the code and needs no extra type. A messenger fits when sender and receiver are not on neighbouring pages (see question 6) |
| Shared keys in `NavigationKeys` | String literals in each ViewModel | DRY, one place to rename |
| `ProductSummary` (record with id, name, barcode, quantity, characteristic values) replaces the old `ProductListItem` and lives in `Services` | Keep the row type in `ViewModels` | The sorter and the catalog (Services) need it; Services must not depend on ViewModels |
| `IProductCatalog` builds `ProductSummary` and `ProductDetails` (read side) | Each ViewModel joins several repositories | The list and the detail page need the same joins; ViewModels stay small |
| `ICharacteristicService` + `ICharacteristicValidator` hold the rules (Manager only, unique name ignoring case, Number parses with the invariant culture, empty value removes it) | Rules in the ViewModels | Testable without UI; the ViewModel only mirrors the validator for friendly messages. `SetValuesAsync` validates every value first, so one bad value changes nothing |
| `CharacteristicValue` helper (parse / normalise) | Parse in validator, service and sorter separately | DRY: one definition of "a number" |
| `IProductCharacteristicRepository` gets `UpdateAsync` and `DeleteAsync` | Mutate the object and rely on in-memory references | Works the same with SQLite in Lab 4 |
| `ProductSorter` is a static pure class; missing values sort last in both directions; ties by name | Sort in the ViewModel | Testable; "missing last" keeps useful rows on top when sorting descending |
| The list reloads only for a new signed-in user; it does not reload in every `OnAppearing` | Reload every time (Lab 1 behaviour) | The lab asks for an update through parameters, not a reload workaround. Every page that changes data returns it: the form returns the new product, the detail page returns the current one |
| Toolbar back arrow and hardware back are bound to `GoBackCommand` | Default Shell back | Plain Shell back sends no parameters, so the list would miss changes (for example a recorded operation) |
| Operation page returns with plain `".."`; the detail page reloads in `OnAppearing` | Send the operation back as a parameter | The detail page owns its data and reads it from the services, so it is always correct |
| Characteristics page reports `CharacteristicsChanged` so the list adds the new sort options | List reloads sort options every time | Same pattern as the product update, no extra work when nothing changed |
| `Stepper` for units on the operation page | `Entry` with parsing | No parsing errors; the service still rejects quantities of 0 or less |

## 5. Changes in the PR

- `Services/`: `INavigationService` (parameters), `Routes`, `NavigationKeys`,
  `ProductSummary`, `ProductDetails`, `CharacteristicValueItem`,
  `OperationHistoryItem`, `IProductCatalog` / `ProductCatalog`,
  `ICharacteristicService` / `CharacteristicService`,
  `ICharacteristicValidator` / `CharacteristicValidator`, `CharacteristicValue`,
  `ProductSorter`, `SortOption`, `SortKind`.
- `Data/`: `IProductCharacteristicRepository.UpdateAsync` / `DeleteAsync`
  (in-memory implementation, `InMemoryRepository` helpers).
- `ViewModels/`: `ProductListViewModel` (sorting, `OpenDetailsCommand`,
  `IQueryAttributable`), `ProductFormViewModel` (returns the new product),
  `ProductDetailViewModel`, `CharacteristicEditItem`, `CharacteristicsViewModel`,
  `OperationViewModel`.
- `Views/`: `ProductDetailPage`, `CharacteristicsPage`, `OperationPage`;
  `ProductsPage` updated. Style `SectionLabel` added.
- Tests: the `ProductListViewModel` test setup follows the new constructor;
  `FakeNavigationService` records parameters. New tests are added in a
  separate commit (see the PR description).

## 6. Open questions / next steps

- Lab 3 opens `OperationPage` from the barcode scanner with the same
  `SelectedProduct` parameter, so the operation page needs no change.
- Editing a product's name or barcode is out of scope.
- Visual design comes later from Figma; the pages use plain shared styles.

## 7. Control questions

**1. What is a route in Shell navigation and which method registers it?**
A route is a unique text identifier of a page, used as a URI by
`GoToAsync("route")`. Pages that are part of the visual tree get their route
from `Route="..."` in `AppShell.xaml` (`login`, `products`); pages that are
pushed onto the stack are registered in code with `Routing.RegisterRoute`.
In StockRoom the constructor of `AppShell` registers `productform`,
`productdetail`, `characteristics` and `operation`, using the constants of
`Routes` so the name exists once.

**2. Why start navigation from an `ICommand` in the ViewModel and not from code-behind?**
A click handler in code-behind ties the navigation rule to one page and to
`Shell`, so it cannot be unit-tested and the logic is split between View and
ViewModel. With a command, the View only binds (`Command="{Binding ...}"`);
the ViewModel decides when and where to go and calls `INavigationService`, which
is replaced by `FakeNavigationService` in tests. Example:
`ProductListViewModel.OpenDetailsCommand` is tested without any UI, and the
command can disable itself through `CanExecute` (for example `AddProductCommand`
for non-managers).

**3. What is the difference between passing parameters in the query string and through `IQueryAttributable`?**
A query string (`productdetail?id=...`) carries only strings; the receiver
declares `[QueryProperty(nameof(Id), "id")]` and has to load the object again by
that id. It is good for simple values, such as an id used in a deep link (Lab 3's
`stockroom://product/{barcode}`). `IQueryAttributable` receives a dictionary of
objects, so a complete `ProductSummary` can be passed with no serialisation and
the page can show it at once; it is good for rich objects that already exist in
memory. We use it for `SelectedProduct` and `UpdatedProduct`. The cost is that
such a call cannot be written as a plain URI.

**4. Which call goes back, and how are parameters passed when going back?**
`GoToAsync("..")` pops the current page (`Routes.Back`). To return data, use the
overload with a dictionary: `GoToAsync("..", parameters)`. The page we return to
receives it in `ApplyQueryAttributes`. `ProductDetailViewModel.SaveCommand` does
`navigation.GoToAsync(Routes.Back, { "UpdatedProduct": summary })` and
`ProductListViewModel.ApplyQueryAttributes` replaces the row.

**5. What happens when `GoToAsync` is called with a route that was not registered?**
Shell cannot resolve the route and throws an `ArgumentException` ("unable to
figure out route for ..."); the navigation does not happen and, unhandled, the
exception reaches the user as a crash. That is why routes are constants in
`Routes` and are all registered in one place, `AppShell`.

**6. What can replace query parameters for sharing data between ViewModels of different pages, and when is it better?**
`WeakReferenceMessenger` (CommunityToolkit.Mvvm) or the older `MessagingCenter`:
a ViewModel sends a message such as `ProductUpdated(product)`, and any
registered ViewModel receives it, whether or not the pages are neighbours.
It is better when the sender and receiver are not connected by one navigation
step, when several pages must react (for example a list and a dashboard), or
when data changes while the receiver is not on screen. A shared singleton
service with state is another option. We chose parameters because the list and
the detail page are direct neighbours; the messenger would add a hidden link
between ViewModels.
