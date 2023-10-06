# Entity Framework Learning

## Primary Key

- If you set one of your domain field names to `Id` or `<CurrentTableName>Id`, then EF will automatically pick up on that and set it as the Primary Key

## Foreign Key

- To set a foreign key, use the naming convention `<ForeignTableName>Id` along with a separate `public virtual` field with the `Type` and `Name` of the foreign table like so:
  ```cs
  public class Team
  {
    public int Id { get; set; }
    public string Name { get; set; }
    // Foreign key
    public int LeagueId { get; set; }
    // Foreign table
    public virtual League League { get; set; }
  }
