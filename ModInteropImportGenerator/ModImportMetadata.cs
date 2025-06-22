namespace ModInteropImportGenerator;

public readonly record struct ModImportMetadata
{
    public ModImportMetadata(string importName, bool requiredDependency)
    {
        ImportName = importName;
        RequiredDependency = requiredDependency;
    }

    public readonly string ImportName;
    public readonly bool RequiredDependency;
}
