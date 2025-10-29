# QueryKit 🎛️

QueryKit is a .NET library that makes it easier to query your data by providing a fluent and intuitive syntax for filtering and sorting. The main use case is a lighter weight subset of OData or GraphQL for parsing external filtering and sorting inputs to provide more granular consumption (e.g. a React UI provides filtering controls to filter a worklist). It's inspired by [Sieve](https://github.com/Biarity/Sieve), but with some differences.

## Getting Started

```bash
dotnet add package QueryKit
```

If we wanted to apply a filter to a `DbSet` called `People`, we just have to do something like this:

```c#
var filterInput = """FirstName == "Jane" && Age > 10""";
var people = _dbContext.People
  	.ApplyQueryKitFilter(filterInput)
  	.ToList();
```

QueryKit will automatically translate this into an expression for you. You can even customize your property names:

```c#
var filterInput = """first == "Jane" && Age > 10""";
var config = new QueryKitConfiguration(config =>
{
    config.Property<Person>(x => x.FirstName).HasQueryName("first");
});
var people = _dbContext.People
  	.ApplyQueryKitFilter(filterInput, config)
  	.ToList();
```

Sorting works too:

```c#
var filterInput = """first == "Jane" && Age > 10""";
var config = new QueryKitConfiguration(config =>
{
    config.Property<Person>(x => x.FirstName).HasQueryName("first");
});
var people = _dbContext.People
  	.ApplyQueryKitFilter(filterInput, config)
  	.ApplyQueryKitSort("first, Age desc", config)
  	.ToList();
```

And that's the basics! There's no services to inject or global set up to worry about, just apply what you want and call it a day. For a full list of capables, see below.

## Filtering

### Usage

To apply filters to your queryable, you just need to pass an input string with your filtering input to `ApplyQueryKitFilter` off of a queryable:

```c#
var people = _dbContext.People.ApplyQueryKitFilter("Age < 10").ToList();
```

You can also pass a configuration like this. More on configuration options below.

```c#
var config = new QueryKitConfiguration(config =>
{
    config.Property<SpecialPerson>(x => x.FirstName)
            .HasQueryName("first")
      		.PreventSort();
});
var people = _dbContext.People
  		.ApplyQueryKitFilter(@$"first == "Jane" && Age < 10", config)
  		.ToList();
```

### Logical Operators

When filtering, you can use logical operators `&&` for `and` as well as `||` for `or`. For example:

```c#
var input = """FirstName == "Jane" && Age < 10""";
var input = """FirstName == "Jane" || FirstName == "John" """;
```

### Order of Operations

You can use order of operation with parentheses like this:

```c#
var input = """(FirstName == "Jane" && Age < 10) || FirstName == "John" """;
```

### Comparison Operators

There's a wide variety of comparison operators that use the same base syntax as [Sieve](https://github.com/Biarity/Sieve)'s operators. To do a case-insensitive operation, just append a ` *` at the end of the operator.

| Name                  | Operator | Case Insensitive Operator | Count Operator |
| --------------------- | -------- | ------------------------- | -------------- |
| Equals                | ==       | ==*                       | #==            |
| Not Equals            | !=       | !=*                       | #!=            |
| Greater Than          | >        | N/A                       | #>             |
| Less Than             | <        | N/A                       | #<             |
| Greater Than Or Equal | >=       | N/A                       | #>=            |
| Less Than Or Equal    | <=       | N/A                       | #<=            |
| Starts With           | _=       | _=*                       | N/A            |
| Does Not Start With   | !_=      | !_=*                      | N/A            |
| Ends With             | _-=      | _-=*                      | N/A            |
| Does Not End With     | !_-=     | !_-=*                     | N/A            |
| Contains              | @=       | @=*                       | N/A            |
| Does Not Contain      | !@=      | !@=*                      | N/A            |
| Sounds Like           | ~~       | N/A                       | N/A            |
| Does Not Sound Like   | !~       | N/A                       | N/A            |
| Has                   | ^$       | ^$*                       | N/A            |
| Does Not Have         | !^$      | !^$*                      | N/A            |
| In                    | ^^       | ^^*                       | N/A            |
| Not In                | !^^      | !^^*                      | N/A            |

> `Sounds Like` and `Does Not Sound Like` requires a soundex configuration on your DbContext. For more info see [the docs below](#soundex)

Here's an example for the `in` operator:

```c#
var input = """(Age ^^ [20, 30, 40]) && (BirthMonth ^^* ["January", "February", "March"]) || (Id ^^ ["6d623e92-d2cf-4496-a2df-f49fa77328ee"])""";
```

### Filtering Notes

* `string` and `guid` properties should be wrapped in double quotes

  * `null` doesn't need quotes: `var input = "Title == null";`

  * Double quotes can be escaped by using a similar syntax to raw-string literals introduced in C#11:

    ```c#
    var input = """""Title == """lamb is great on a "gee-ro" not a "gy-ro" sandwich""" """"";
    // OR 
    var input = """""""""Title == """"lamb is great on a "gee-ro" not a "gy-ro" sandwich"""" """"""""";
    ```

* Dates and times use ISO 8601 format and should be surrounded by double quotes:

  * `DateOnly`: `var filterInput = """Birthday == "2022-07-01" """;`
  * `DateTimeOffset`: 
    * `var filterInput = """Birthday == "2022-07-01T00:00:03Z" """;` 
  * `DateTime`: `var filterInput = """Birthday == "2022-07-01" """;`
    * `var filterInput = """Birthday == "2022-07-01T00:00:03" """;` 
    * `var filterInput = """Birthday == "2022-07-01T00:00:03+01:00" """;` 

  * `TimeOnly`: 
    * `var filterInput = """Time == "12:30:00" """;`
    * `var filterInput = """Time == "12:30:00.678722" """;`

* `bool` properties need to use `== true`, `== false`, or the same using the `!=` operator. they can not be standalone properies: 

  * ❌ `var input = """Title == "chicken & waffles" && Favorite""";` 
  * ✅ `var input = """Title == "chicken & waffles" && Favorite == true""";` 

#### Complex Example

```c#
var input = """(Title == "lamb" && ((Age >= 25 && Rating < 4.5) || (SpecificDate <= "2022-07-01T00:00:03Z" && Time == "00:00:03")) && (Favorite == true || Email.Value _= "hello@gmail.com"))""";
```

### Property-to-Property Comparisons

QueryKit supports comparing one property directly to another property on the same entity. This allows for dynamic filtering where the comparison value is another field rather than a literal value:

```c#
// Compare two string properties
var input = """FirstName == LastName""";

// Compare numeric properties  
var input = """Age > Rating""";

// Use in complex expressions
var input = """(FirstName != LastName && Age > Rating) || (Score1 <= Score2)""";

// Combine with regular filters
var input = """FirstName == LastName && Age > 21""";
```

Property-to-property comparisons work with all comparison operators and automatically handle type conversions between compatible numeric types (e.g., comparing `int` with `decimal`).

#### Child Property Comparisons

You can also compare child properties to root properties or other child properties:

```c#
// Compare child property to root property
var input = """Author.Name == Title""";

// Compare nested child properties
var input = """Email.Value == CollectionEmail.Value""";

// Mix child and root properties in complex expressions
var input = """Author.Name != Title && Rating > Author.Score""";
```

Child property comparisons work with:
- **Nested Objects**: `Author.Name`, `Email.Value`
- **All Operators**: `==`, `!=`, `>`, `<`, `>=`, `<=`
- **Type Conversion**: Automatic conversion between compatible types
- **Complex Expressions**: Can be combined with logical operators and parentheses

### Arithmetic Expressions

QueryKit supports arithmetic expressions in filters, allowing you to perform calculations directly within your queries. This enables powerful filtering capabilities based on computed values.

#### Basic Arithmetic Operations

```c#
// Addition: Find records where Age + Rating is greater than 50
var input = "(Age + Rating) > 50";

// Subtraction: Find records where Age - Rating is positive
var input = "(Age - Rating) > 0";

// Multiplication: Find records where Price * Quantity exceeds 1000
var input = "(Price * Quantity) > 1000";

// Division: Find records where Total / Count is less than 100
var input = "(Total / Count) < 100";

// Modulo: Find records where ID is even
var input = "(Id % 2) == 0";
```

#### Operator Precedence

Arithmetic expressions follow standard mathematical operator precedence:

```c#
// Multiplication and division before addition and subtraction
var input = "(Price + Tax * Rate) > 100";  // Tax * Rate is calculated first

// Use parentheses to override precedence
var input = "((Price + Tax) * Rate) > 100"; // Addition happens first
```

#### Mixing Properties and Literals

You can combine entity properties with literal numeric values:

```c#
// Property with literal
var input = "(Age - 18) >= 0";   // Age minus 18

// Multiple properties with literals
var input = "(Price * 1.1 + ShippingCost) <= Budget";
```

#### Complex Arithmetic Expressions

Arithmetic expressions can be combined with logical operators and used in complex scenarios:

```c#
// Arithmetic with logical operators
var input = """(Price - Cost) > 300 && Category == "Electronics" """;

// Multiple arithmetic comparisons
var input = "(Score1 + Score2) > 150 && (Score1 - Score2) < 20";

// Nested arithmetic expressions
var input = "((Revenue - Expenses) / Revenue) > 0.1";
```

#### Supported Features

- **All Numeric Types**: `int`, `decimal`, `double`, `float`, `long`, `short`, `byte` and their nullable variants
- **Automatic Type Conversion**: Compatible numeric types are automatically converted for calculations
- **Parentheses**: Use parentheses to control calculation order and group expressions
- **Entity Framework Translation**: All arithmetic expressions are translated to efficient SQL queries
- **Property-to-Property**: Can mix property references with literal values in the same expression

#### Examples

```c#
// Calculate profit margin and filter
var profitableItems = _dbContext.Products
    .ApplyQueryKitFilter("((Price - Cost) / Price) > 0.2")
    .ToList();

// Find orders with high shipping ratio
var expensiveShipping = _dbContext.Orders
    .ApplyQueryKitFilter("(ShippingCost / TotalAmount) > 0.15")
    .ToList();

// Complex business logic in one filter
var qualifiedCustomers = _dbContext.Customers
    .ApplyQueryKitFilter("""
        (TotalPurchases / NumberOfOrders) > 500 && 
        ((LastOrderDate - FirstOrderDate) / 365) >= 2
    """)
    .ToList();
```

#### Filtering Projections
You can also filter on queryable projections like so:
```csharp
var input = $"""info @=* "{fakeAuthorOne.Name}" """;

var config = new QueryKitConfiguration(config =>
{
    config.Property<RecipeDto>(x => x.AuthorInfo).HasQueryName("info");
});

var queryableRecipe = testingServiceScope.DbContext().Recipes
    .Include(x => x.Author)
    .Select(x => new RecipeDto
    {
        Id = x.Id,
        Title = x.Title,
        AuthorName = x.Author.Name,
        AuthorInfo = x.Author.Name + " - " + x.Author.InternalIdentifier
    });
var appliedQueryable = queryableRecipe.ApplyQueryKitFilter(input, config);
var recipes = await appliedQueryable.ToListAsync();
```

#### Filtering Collections

You can also filter into collections with QueryKit by using most of the normal operators. For example, if I wanted to filter for recipes that only have an ingredient named `salt`, I could do something like this:

```csharp
var input = """"Ingredients.Name == "salt" """";
```

By default, QueryKit will use `Any` under the hood when building this filter, but if you want to use `All`, you just need to prefix the operator with a `%`:

```csharp
var input = """"Ingredients.Stock %>= 1"""";
```

Nested collections can also be filtered:

```csharp	
var input = $"""preparations == "{preparationOne.Text}" """;
var config = new QueryKitConfiguration(settings =>
{
    settings.Property<Recipe>(x => x.Ingredients
        .SelectMany(y => y.Preparations)
        .Select(y => y.Text))
        .HasQueryName("preparations");
});

// Act
var queryableRecipes = testingServiceScope.DbContext().Recipes;
var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input, config);
var recipes = await appliedQueryable.ToListAsync();
```

If you want to filter a primitve collection like `List<string>` you can use the `Has` or `DoesNotHave` operator (can be case insensitive with the appended `*`):

```csharp
var input = """Tags ^$ "winner" """;
// or
var input = """Tags !^$ "winner" """;
```

If you want to filter on the count of a collection, you can prefix some of the operators with a `#`. For example, if i wanted to get all recipes that have more than 0 ingredients:

```csharp
var input = """"Ingredients #>= 0"""";
```

#### Filtering Enums

You can filter enums with their respective integer value:

```csharp
var input = "BirthMonth == 1";

public enum BirthMonthEnum
{
    January = 1,
    February = 2,
    //...
}
```

### Settings

#### Property Settings

Filtering is set up to create an expression using the property names you have on your entity, but you can pass in a config to customize things a bit when needed.

* `HasQueryName()` to create a custom alias for a property. For exmaple, we can make `FirstName` aliased to `first`.
* `PreventFilter()` to prevent filtering on a given property

```c#
var input = $"""first == "Jane" || Age > 10""";
var config = new QueryKitConfiguration(config =>
{
    config.Property<SpecialPerson>(x => x.FirstName)
            .HasQueryName("first");
    config.Property<SpecialPerson>(x => x.Age)
      		.PreventFilter();
});
```

#### Derived Properties

You can also expose custom derived properties for consumption. Just be sure that Linq can handle them in a db query if you're using it that way.

```csharp
var config = new QueryKitConfiguration(config =>
{
    config.DerivedProperty<Person>(p => p.FirstName + " " + p.LastName).HasQueryName("fullname");
    config.DerivedProperty<Person>(p => p.Age >= 18 && p.FirstName == "John").HasQueryName("adult_johns");
});

var input = $"""(fullname @=* "John Doe") && age >= 18""";
// or
var input = $"""adult_johns == true""";
```

#### Custom Operations

For more complex business logic that can't be expressed as simple derived properties, you can define custom operations. These allow you to encapsulate sophisticated filtering logic that can access related data, perform calculations, or implement domain-specific rules.

```csharp
var config = new QueryKitConfiguration(config =>
{
    // Define a custom operation that checks if a book has sold more than X units in the last 10 days
    config.CustomOperation<Book>((x, op, value) => 
        x.Orders.Where(y => y.OrderDate > DateTime.UtcNow.AddDays(-10))
               .Sum(o => o.Quantity) > (int)value)
        .HasQueryName("SoldUnitsMoreThan10Days");
    
    // Custom operation for VIP customer detection
    config.CustomOperation<Customer>((x, op, value) => 
        (bool)value ? 
            (x.TotalPurchases > 10000 && x.AccountAge > 365 && x.FirstName.Contains("VIP")) :
            !(x.TotalPurchases > 10000 && x.AccountAge > 365 && x.FirstName.Contains("VIP")))
        .HasQueryName("isVipCustomer");
    
    // Custom operation with date parameter handling
    config.CustomOperation<Order>((x, op, value) => 
        x.OrderDate > (DateTime)value)
        .HasQueryName("isAfterDate");
});

// Usage examples:
var input = """SoldUnitsMoreThan10Days > 100""";        // Books that sold more than 100 units
var input = """isVipCustomer == true""";                // VIP customers only
var input = """isAfterDate == "2023-06-15T00:00:00Z" """; // Orders after specific date
```

**Custom Operation Features:**

- **Business Logic Encapsulation**: Complex domain logic can be centralized and reused
- **Related Data Access**: Can navigate to related entities and collections (e.g., `x.Orders`, `x.Items`)
- **Operator Access**: The operation receives the comparison operator being used
- **Type-Safe Parameters**: Automatic conversion of string values to appropriate types (bool, int, decimal, DateTime, etc.)
- **Entity Framework Compatible**: Generated expressions translate to efficient SQL queries

**Common Use Cases:**

- **Performance Metrics**: Calculate efficiency ratios, averages, or complex aggregations
- **Business Intelligence**: Revenue calculations, customer scoring, inventory analysis
- **Time-Based Logic**: Recent activity checks, age calculations, expiration rules
- **Customer Segmentation**: VIP status, loyalty tiers, purchase behavior patterns
- **Quality Control**: Average ratings, compliance checks, threshold validations

**Parameter Type Conversion:**

Custom operations automatically handle common data types:

```csharp
// Boolean parameters
var input = """isEligible == true""";      // Converts "true" to bool

// Numeric parameters  
var input = """totalScore > 85.5""";       // Converts "85.5" to decimal/double
var input = """itemCount >= 10""";         // Converts "10" to int

// Date parameters
var input = """lastLoginAfter == "2023-12-01T00:00:00Z" """; // Converts to DateTime

// String parameters (with quotes)
var input = """categoryMatches == "electronics" """; // Keeps as string
```

Custom operations provide a powerful way to extend QueryKit's filtering capabilities while maintaining type safety and Entity Framework compatibility.

#### Custom Operators

You can also add custom comparison operators to your config if you'd like:
```csharp
var config = new QueryKitConfiguration(config =>
{
    config.EqualsOperator = "@@$";
    config.CaseInsensitiveAppendix = "$";
    config.AndOperator = "and";
});
```

If you want to use it globally, you can make a base implementation like this:

```csharp
public class CustomQueryKitConfiguration : QueryKitConfiguration
{
    public CustomQueryKitConfiguration(Action<QueryKitSettings>? configureSettings = null)
        : base(settings => 
        {
            settings.EqualsOperator = "eq";
            settings.NotEqualsOperator = "neq";
            settings.GreaterThanOperator = "gt";
            settings.GreaterThanOrEqualOperator = "gte";
            settings.LessThanOperator = "lt";
            settings.LessThanOrEqualOperator = "lte";
            settings.ContainsOperator = "ct";
            settings.StartsWithOperator = "sw";
            settings.EndsWithOperator = "ew";
            settings.NotContainsOperator = "nct";
            settings.NotStartsWithOperator = "nsw";
            settings.NotEndsWithOperator = "new";
            settings.AndOperator = "and";
            settings.OrOperator = "or";
            settings.CaseInsensitiveAppendix = "i";

            configureSettings?.Invoke(settings);
        })
    {
    }
}

// ---

var input = """Title eq$ "Pancakes" and Rating gt 10""";
var config = new CustomQueryKitConfiguration();
var filterExpression = FilterParser.ParseFilter<Recipe>(input, config);
```

> **Note**
> Spaces must be used around the comparison operator when using custom values.
> `Title @@$ "titilating"` ✅ 
> `Title@@$"titilating"` ❌

#### Allow Unknown Properties

By default, QueryKit will throw an error if it doesn't recognize a property name, If you want to loosen the reigns here a bit, you can set `AllowUnknownProperties` to `true` in your config. When active, unknown properties will be ignored in the expression resolution.
```csharp
var config = new QueryKitConfiguration(config =>
{
    config.AllowUnknownProperties = true;
});
var filterExpression = FilterParser.ParseFilter<Recipe>(input, config);
```

### Nested Objects

Say we have a nested object like this:

```C#

public class SpecialPerson
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public EmailAddress Email { get; set; }
}

public class EmailAddress : ValueObject
{
    public EmailAddress(string? value)
    {
        Value = value;
    }
    
    public string? Value { get; private set; }
}
```

To actually use the nested properties, you can do something like this:

```c#
var input = $"""Email.Value == "{value}" """;

// or with an alias...
var input = $"""email == "hello@gmail.com" """;
var config = new QueryKitConfiguration(config =>
{
    config.Property<SpecialPerson>(x => x.Email.Value).HasQueryName("email");
});
```

Note, with EF core, your QueryKit configuration depends on how you've configured the property:

```c#
public sealed class PersonConfiguration : IEntityTypeConfiguration<SpecialPerson>
{
    public void Configure(EntityTypeBuilder<SpecialPerson> builder)
    {
        builder.HasKey(x => x.Id);

      	// Option 1 (as of .NET 8) - ComplexProperty
      	// QueryKit: config.Property<SpecialPerson>(x => x.Email.Value).HasQueryName("email");
      	builder.ComplexProperty(x => x.Email,
            x => x.Property(y => y.Value)
                .HasColumnName("email"));

      	// Option 2 - HasConversion (see HasConversion support below)
      	// QueryKit: config.Property<SpecialPerson>(x => x.Email).HasQueryName("email").HasConversion<string>();
        builder.Property(x => x.Email)
            .HasConversion(x => x.Value, x => new EmailAddress(x))
            .HasColumnName("email");

        // Option 3 - OwnsOne
        // QueryKit: config.Property<SpecialPerson>(x => x.Email.Value).HasQueryName("email");
        builder.OwnsOne(x => x.Email, opts =>
        {
            opts.Property(x => x.Value).HasColumnName("email");
        }).Navigation(x => x.Email)
            .IsRequired();
    }
}
```

**Key Distinction:**
- **HasConversion**: Use `x => x.Email` in QueryKit (point to parent property)
- **ComplexProperty/OwnsOne**: Use `x => x.Email.Value` in QueryKit (point to nested property)

### HasConversion Support

For properties configured with EF Core's `HasConversion`, QueryKit provides special support that allows you to filter against the property directly without needing to access nested values. Use the `HasConversion<TTarget>()` configuration method:

```c#
// EF configuration with HasConversion
builder.Property(x => x.Email)
    .HasConversion(x => x.Value, x => new EmailAddress(x))
    .HasColumnName("email");

// QueryKit configuration for HasConversion properties
var config = new QueryKitConfiguration(config =>
{
    config.Property<SpecialPerson>(x => x.Email)  // Point to Email property, NOT Email.Value
        .HasQueryName("email")
        .HasConversion<string>(); // Specify the target type used in HasConversion
});

// Now you can filter directly against the property:
var input = """email == "hello@gmail.com" """;
var people = _dbContext.People
    .ApplyQueryKitFilter(input, config)
    .ToList();
```

This allows you to use `Email == "value"` syntax instead of `Email.Value == "value"` when the property is configured with HasConversion in EF Core. The `HasConversion<TTarget>()` method tells QueryKit what the conversion target type is so it can handle the type conversion properly.

> **Important:** When using `HasConversion` in EF Core, you MUST configure the property in QueryKit using `x => x.Email`, not `x => x.Email.Value`. The conversion is on the parent property, so pointing to the nested `.Value` property will cause EF Core translation errors. Use `x => x.Email.Value` only when using `ComplexProperty` or `OwnsOne` without HasConversion.

## Sorting

Sorting is a more simplistic flow. It's just an input with a comma delimited list of properties to sort by. 

### Rules

* use `asc` or `desc` to designate if you want it to be ascending or descending. If neither is used, QueryKit will assume `asc`
* You can use Sieve syntax as well by prefixing a property with `-` to designate it as `desc`
* Spaces after commas are optional

So all of these are valid:

```c#
var input = "Title";
var input = "Title, Age desc";
var input = "Title desc, Age desc";
var input = "Title, Age";
var input = "Title asc, -Age";
var input = "Title, -Age";
```

### Property Settings

Sorting is set up to create an expression using the property names you have on your entity, but you can pass in a config to customize things a bit when needed.

* Just as with filtering, `HasQueryName()` to create a custom alias for a property. For exmaple, we can make `FirstName` aliased to `first`.
* `PreventSort()` to prevent filtering on a given property

```c#
var input = $"""Age desc, first"";
var config = new QueryKitConfiguration(config =>
{
    config.Property<SpecialPerson>(x => x.FirstName)
          .HasQueryName("first")
          .PreventSort();
});
```

## Aggregate QueryKit Application

If you want to apply filtering and sorting in one fell swoop, you can do something like this:

```csharp
var config = new QueryKitConfiguration(config =>
{
    config.Property<Person>(x => x.FirstName).HasQueryName("first");
});
var people = _dbContext.People
  	.ApplyQueryKit(new QueryKitData() 
        {
            Filters = """first == "Jane" && Age > 10""",
            SortOrder = "first, Age desc",
            Configuration = config
        })
  	.ToList();
```

## Using QueryKit on Enumerables
Since QueryKit is really just a parser for expressions, you can use it on any `IEnumerable<T>` as well. Just be sure to use the `ApplyQueryKitFilter` and `ApplyQueryKitSort` methods off of the enumerable.

For example
```csharp
var recipeOne = new FakeRecipeBuilder().Build();
var recipeTwo = new FakeRecipeBuilder().Build();
var listOfRecipes = new List<Recipe> { recipeOne, recipeTwo };

var input = $"""{nameof(Recipe.Title)} == "{recipeOne.Title}" """;

var filteredRecipes = listOfRecipes.ApplyQueryKitFilter(input).ToList();
````


## Error Handling

If you want to capture errors to easily throw a `400`, you can add error handling around these exceptions:

* A `QueryKitException` is the base class for all of the exceptions listed below. This can be caught to catch
any exception thrown by QueryKit.
* A `ParsingException` will be thrown when there is an invalid operator or bad syntax is used (e.g. not using double quotes around a string or guid).
* An `UnknownFilterPropertyException` will be thrown if a property is not recognized during filtering
* A `SortParsingException` will be thrown if a property or operation is not recognized during sorting
* A `QueryKitDbContextTypeException` will be thrown when trying to use a `DbContext` specific workflow without passing that context (e.g. SoundEx)
* A `SoundsLikeNotImplementedException` will be thrown when trying to use `soundex` on a `DbContext` that doesn't have it implemented.
* A `QueryKitParsingException` is a more generic error that will include specific details on a more granular error in the parsing pipeline.

## SoundEx

The `Sounds Like` and `Does Not Sound Like` operators require a soundex configuration on any `DbContext` that contain your `DbSet` being filtered on. Something like the below should work. The `SoundsLike` method does not need to implement anything and is just used as a pointer to the db method.

```csharp
public class ExampleDbContext : DbContext
{
    public ExampleDbContext(DbContextOptions<TestingDbContext> options)
        : base(options)
    {
    }
    
    [DbFunction (Name = "SOUNDEX", IsBuiltIn = true)]
    public static string SoundsLike(string query) => throw new NotImplementedException();

    public DbSet<People> MyPeople { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("fuzzystrmatch");
    }
}
```

> ⭐️ Note that with Postgres, something like `modelBuilder.HasPostgresExtension("fuzzystrmatch");` will need to be added like the example along with a migration for adding the extension.

You can even use this on a normal `IQueryable` like this: 
```csharp
var waffleRecipes = _dbContext.MyPeople
  .Where(x => ExampleDbContext.SoundsLike(x.LastName) == ExampleDbContext.SoundsLike("devito"))
  .ToList();
```

### Usage

Once your `DbContext` is configured to allow soundex, you'll need to provide that `DbContext` type in your QueryKit config. This is because, as of now, there is no reliable way to get the `DbContext` from an `IQueryable`.

```csharp
var input = $"""LastName ~~ "devito" """;

// Act
var queryablePeople = testingServiceScope.DbContext().People;
var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, new QueryKitConfiguration(o =>
{
    o.DbContextType = typeof(TestingDbContext);
}));
var people = await appliedQueryable.ToListAsync();
```

## Community Projects
- [Fluent QueryKit](https://github.com/CLFPosthumus/fluent-querykit) for easy usage in javascript or typescript projects.
