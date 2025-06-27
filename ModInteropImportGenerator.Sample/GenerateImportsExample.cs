using ModInteropImportGenerator.Sample.Stubs;

namespace ModInteropImportGenerator.Sample;

[GenerateImports("XYZ", RequiredDependency = true)]
public static partial class GenerateImportsExample
{
    public static partial void A();
    public static partial object B();
    public static partial int C();
    public static partial ComponentStub D();

    public static partial void E(object argumentA, int argumentB);
    public static partial object F(object argumentA, int argumentB);
    public static partial int G(object argumentA, int argumentB);
    public static partial ComponentStub I(object argumentA, int argumentB);
    public static partial ComponentStub I(object argumentA, int argumentB, string argumentC);

    public static partial void J(ref string argumentA);
    public static partial void K(out string argumentA);
    public static partial void L(in string argumentA);
    public static partial void M(ref readonly string argumentA);
    public static partial void N(int argumentA, ref int argumentB, out int argumentC, in int argumentD, ref readonly string argumentE);
    public static partial object O(int argumentA, ref int argumentB, out int argumentC, in int argumentD, ref readonly string argumentE);
}
