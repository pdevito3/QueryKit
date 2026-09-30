using QueryKit.Configuration;

namespace QueryKitVerify.Driver;

/// <summary>
/// Named QueryKitConfiguration presets, selected with --config. Each preset mirrors a README settings example.
/// Add a preset here when a feature file needs a configuration that none of these cover.
/// </summary>
public static class Configs
{
    public static readonly Dictionary<string, (string Description, Func<QueryKitConfiguration?> Build)> All = new()
    {
        ["none"] = ("No configuration (default parser behavior).", () => null),

        ["aliases"] = ("HasQueryName: Title->name, Author.Name->chef. Rating PreventFilter. Price PreventSort.",
            () => new QueryKitConfiguration(s =>
            {
                s.Property<Recipe>(x => x.Title).HasQueryName("name");
                s.Property<Recipe>(x => x.Author.Name).HasQueryName("chef");
                s.Property<Recipe>(x => x.Rating).PreventFilter();
                s.Property<Recipe>(x => x.Price).PreventSort();
            })),

        ["loose-names"] = ("HasQueryName with text that is not an identifier: Title->recipe-title, Rating->_stars, Author.Name->chef name.",
            () => new QueryKitConfiguration(s =>
            {
                s.Property<Recipe>(x => x.Title).HasQueryName("recipe-title");
                s.Property<Recipe>(x => x.Rating).HasQueryName("_stars");
                s.Property<Recipe>(x => x.Author.Name).HasQueryName("chef name");
            })),

        ["derived"] = ("DerivedProperty: headline = Title + \" by \" + Author.Name, top_rated = Rating >= 4.",
            () => new QueryKitConfiguration(s =>
            {
                s.DerivedProperty<Recipe>(x => x.Title + " by " + x.Author.Name).HasQueryName("headline");
                s.DerivedProperty<Recipe>(x => x.Rating >= 4).HasQueryName("top_rated");
            })),

        ["custom-operation"] = ("CustomOperation: total_stock_above = sum of Ingredients.Stock > value. sku_is = Sku == (string)value.",
            () => new QueryKitConfiguration(s =>
            {
                s.CustomOperation<Recipe>((x, op, value) => x.Ingredients.Sum(i => i.Stock) > (int)value)
                    .HasQueryName("total_stock_above");
                s.CustomOperation<Recipe>((x, op, value) => x.Sku == (string)value)
                    .HasQueryName("sku_is");
            })),

        ["word-operators"] = ("Custom operators: eq neq gt gte lt lte ct sw ew, and/or, case-insensitive appendix i.",
            () => new QueryKitConfiguration(s =>
            {
                s.EqualsOperator = "eq";
                s.NotEqualsOperator = "neq";
                s.GreaterThanOperator = "gt";
                s.GreaterThanOrEqualOperator = "gte";
                s.LessThanOperator = "lt";
                s.LessThanOrEqualOperator = "lte";
                s.ContainsOperator = "ct";
                s.StartsWithOperator = "sw";
                s.EndsWithOperator = "ew";
                s.AndOperator = "and";
                s.OrOperator = "or";
                s.CaseInsensitiveAppendix = "i";
            })),

        ["hidden-price"] = ("Price PreventFilter and PreventSort, with HasQueryName(\"cost\").",
            () => new QueryKitConfiguration(s =>
            {
                s.Property<Recipe>(x => x.Price).HasQueryName("cost").PreventFilter().PreventSort();
            })),

        ["allow-unknown"] = ("AllowUnknownProperties = true.",
            () => new QueryKitConfiguration(s => s.AllowUnknownProperties = true)),

        ["max-depth-0"] = ("MaxPropertyDepth = 0 (root properties only).",
            () => new QueryKitConfiguration(s => s.MaxPropertyDepth = 0)),

        ["upper"] = ("CaseInsensitiveComparison = Upper.",
            () => new QueryKitConfiguration(s => s.CaseInsensitiveComparison = CaseInsensitiveMode.Upper)),
    };
}
