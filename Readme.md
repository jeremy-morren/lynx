# Lynx

Lynx is a very simple document session abstraction over Entity Framework Core, using bulk extensions underneath to effect the insertions.

### Usage

```csharp
var session = context.OpenSession();
context.Store(new Person { Name = "John Doe" });
context.DeleteWhere<Person>(p => p.Name == "Jane Doe");
context.SaveChanges();
```