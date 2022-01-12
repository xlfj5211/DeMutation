using de4dot.blocks;
using de4dot.blocks.cflow;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System.Collections.Generic;

namespace DeMutation
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string path = args[0];
            ModuleDefMD module = ModuleDefMD.Load(path);
            BlocksCflowDeobfuscator deobfuscator = new BlocksCflowDeobfuscator();
            deobfuscator.Add(new ControlFlowDeobfuscator());
            deobfuscator.Add(new MethodCallInliner(true)); //Added this for my own benefit, you can remove it yourself
            foreach (TypeDef type in module.GetTypes())
            {
                foreach (MethodDef method in type.Methods)
                {
                    if (method.HasBody)
                    {
                        if (method.Body.HasInstructions)
                        {
                            Blocks blocks = new Blocks(method);
                            deobfuscator.Initialize(blocks);
                            deobfuscator.Deobfuscate();
                            blocks.RemoveDeadBlocks();
                            blocks.RepartitionBlocks();
                            blocks.UpdateBlocks();
                            deobfuscator.Deobfuscate();
                            blocks.RepartitionBlocks();
                            IList<Instruction> allInstructions;
                            IList<ExceptionHandler> allExceptionHandlers;
                            blocks.GetCode(out allInstructions, out allExceptionHandlers);
                            DotNetUtils.RestoreBody(method, allInstructions, allExceptionHandlers);
                        }
                    }
                }
            }
            NativeModuleWriterOptions writerOptions = new NativeModuleWriterOptions(module, true);
            writerOptions.Logger = DummyLogger.NoThrowInstance;
            writerOptions.MetadataOptions.Flags = MetadataFlags.PreserveAll;
            module.NativeWrite(path.Replace(".exe", "-demutate.exe"), writerOptions);
        }
    }
}
