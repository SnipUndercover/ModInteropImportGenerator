namespace ModInteropImportGenerator;

public readonly record struct ModImportMetadata(string ImportName, bool RequiredDependency)
{
    public readonly string ImportName = ImportName;
    public readonly bool RequiredDependency = RequiredDependency;
}
