.assembly extern mscorlib { auto }
.assembly TestDll { }
.module TestDll.dll

.vtfixup [1] int64 fromunmanaged at VT_01
.data VT_01 = int64(0)

.class public auto ansi beforefieldinit TestClass extends [mscorlib]System.Object
{
  .method public static int32 MyExport(native int arg) cil managed
  {
    .export [1] as MyExport
    ldc.i4 42
    ret
  }
}
