namespace QueryKit.WebApiTestProject.Entities.Recipes;

using Ardalis.SmartEnum;

public abstract class VisibilityEnum : SmartEnum<VisibilityEnum>
{
    public static readonly VisibilityEnum Public = new PublicType();
    public static readonly VisibilityEnum FriendsOnly = new FriendsOnlyType();
    public static readonly VisibilityEnum Private = new PrivateType();

    protected VisibilityEnum(string name, int value) : base(name, value)
    {
    }

    private class PublicType : VisibilityEnum
    {
        public PublicType() : base("Public", 0)
        {
        }
    }

    private class FriendsOnlyType : VisibilityEnum
    {
        public FriendsOnlyType() : base("Friends Only", 1)
        {
        }
    }

    private class PrivateType : VisibilityEnum
    {
        public PrivateType() : base("Private", 2)
        {
        }
    }
}