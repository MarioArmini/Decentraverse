namespace Decentraverse.Generator;

public static class GeneratorInfo
{
    /// <summary>
    /// Version of the generation rules. It is part of every seed:
    /// changing any rule (tables, order of draws, formulas)
    /// requires bumping it, so the universe never changes silently.
    /// </summary>
    public const string Version = "v1";
}
