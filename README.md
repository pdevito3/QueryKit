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
  		.ApplyQueryKitFilter("first == "Jane" && Age < 10", config)
  		.ToList();
```

### Logical Operators

When filtering, you can use logical operators `&&` for `and` as well as `||` for `or`. For example:

```c#
var input = """FirstName == "Jane" && Age < 10""";
var input = """FirstName == "Jane" || FirstName == "John" """;
```

Additionally, you can use `^^` for an `in` operator. You can add an `*` and use `^^*` for case-insensitivity as well:

```c#
var input = """(Age ^^ [20, 30, 40]) && (BirthMonth ^^* ["January", "February", "March"]) || (Id ^^ ["6d623e92-d2cf-4496-a2df-f49fa77328ee"])""";
```

### Order of Operations

You can use order of operation with parentheses like this:

```c#
var input = """(FirstName == "Jane" && Age < 10) || FirstName == "John" """;
```

### Comparison Operators

There's a wide variety of comparison operators that use the same base syntax as [Sieve](https://github.com/Biarity/Sieve)'s operators. To do a case-insensitive operation, just append a ` *` at the end of the operator.

| Name                  | Operator | Case Insensitive Operator |
| --------------------- | -------- | ------------------------- |
| Equals                | ==       | ==*                       |
| Not Equals            | !=       | !=                        |
| Greater Than          | >        | N/A                       |
| Less Than             | <        | N/A                       |
| Greater Than Or Equal | >=       | N/A                       |
| Less Than Or Equal    | <=       | N/A                       |
| Starts With           | _=       | _=*                       |
| Does Not Start With   | !_=      | !_=*                      |
| Ends With             | _-=      | _-=*                      |
| Does Not End With     | !_-=     | !_-=*                     |
| Contains              | @=       | @=*                       |
| Does Not Contain      | !@=      | !@=*                      |

### Filtering Notes

* `string` and `guid` properties should be wrapped in double quotes

  * `null` doesn't need quotes: `var input = "Title == null";`

  * Double quotes can be escaped by using a similar syntax to raw-string literals introduced in C#11:

    ```c#
    var input = """""Title == """lamb is great on a "gee-ro" not a "gy-ro" sandwich""" """"";
    // OR 
    var input = """""""""Title == """"lamb is great on a "gee-ro" not a "gy-ro" sandwich"""" """"""""";
    ```

* Dates and times use ISO format:

  * `DateOnly`: `var filterInput = "Birthday == 2022-07-01";`
  * `DateTimeOffset`: 
    * `var filterInput = "Birthday == 2022-07-01T00:00:03Z";` 
  * `DateTime`: `var filterInput = "Birthday == 2022-07-01";`
    * `var filterInput = "Birthday == 2022-07-01T00:00:03";` 
    * `var filterInput = "Birthday ==  2022-07-01T00:00:03+01:00";` 

  * `TimeOnly`: `var filterInput = "Time == 12:30:00";`

* `bool` properties need to use `== true`, `== false`, or the same using the `!=` operator. they can not be standalone properies: 

  * ❌ `var input = """Title == "chicken & waffles" && Favorite""";` 
  * ✅ `var input = """Title == "chicken & waffles" && Favorite == true""";` 

#### Complex Example

```c#
var input = """(Title == "lamb" && ((Age >= 25 && Rating < 4.5) || (SpecificDate <= 2022-07-01T00:00:03Z && Time == 00:00:03)) && (Favorite == true || Email.Value _= "hello@gmail.com"))""";
```

### Property Settings

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

Note, with EF core, your config might look like this:

```c#
public sealed class PersonConfiguration : IEntityTypeConfiguration<SpecialPerson>
{
    public void Configure(EntityTypeBuilder<SpecialPerson> builder)
    {
        builder.HasKey(x => x.Id);
        
      // Option 1
        builder.Property(x => x.Email)
            .HasConversion(x => x.Value, x => new EmailAddress(x))
            .HasColumnName("email");      
      
        // Option 2      
        builder.OwnsOne(x => x.Email, opts =>
        {
            opts.Property(x => x.Value).HasColumnName("email");
        }).Navigation(x => x.Email)
            .IsRequired();
    }
}
```

> **Warning**
> EF properties configured with `HasConversion` are not supported at this time -- if this is a blocker for you, i'd love to hear your use case

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

## Error Handling

If you want to capture errors to easily throw a `400`, you can add error handling aronud these exceptions:

* A `FilterParsingException` will be thrown when there is an invalid operator or bad syntax is used (e.g. not using double quotes around a string or guid).
* An `UnknownFilterPropertyException` will be thrown if a property is not recognized during filtering
* A `SortParsingException` will be thrown if a property or operation is not recognized during sorting
