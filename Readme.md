# Lynx

Lynx is a very simple document session abstraction over Entity Framework Core, using bulk extensions underneath to effect the insertions.

### Usage

```csharp
var session = store.OpenSession();
session.Store(new Person { Name = "John Doe" });
session.DeleteWhere<Person>(p => p.Name == "Jane Doe");
session.SaveChanges();
```
