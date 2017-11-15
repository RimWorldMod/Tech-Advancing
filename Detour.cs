using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Verse;

namespace TechAdvancing
{
    class Detour
    {
        public static void DoDetour(MethodInfo source, MethodInfo target)
        {
            if (LogOutput.DebugMode_TA_enabled) { LogOutput.WriteLogMessage(Errorlevel.Warning, "DEBUG MODE ACTIVATED"); }            

            MethodInfo methodToReplace = target;
            MethodInfo methodToInject = source;            

            RuntimeHelpers.PrepareMethod(methodToReplace.MethodHandle);
            RuntimeHelpers.PrepareMethod(methodToInject.MethodHandle);

            unsafe
            {
                if (IntPtr.Size == sizeof(Int64))
                {
                    // 64-bit systems use 64-bit absolute address and jumps
                    // 12 byte destructive

                    // Get function pointers
                    long Source_Base = source.MethodHandle.GetFunctionPointer().ToInt64();
                    long Destination_Base = target.MethodHandle.GetFunctionPointer().ToInt64();

                    // Native source address
                    byte* Pointer_Raw_Source = (byte*)Source_Base;

                    // Pointer to insert jump address into native code
                    long* Pointer_Raw_Address = (long*)(Pointer_Raw_Source + 0x02);

                    // Insert 64-bit absolute jump into native code (address in rax)
                    // mov rax, immediate64
                    // jmp [rax]
                    *(Pointer_Raw_Source + 0x00) = 0x48;
                    *(Pointer_Raw_Source + 0x01) = 0xB8;
                    *Pointer_Raw_Address = Destination_Base; // ( Pointer_Raw_Source + 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 )
                    *(Pointer_Raw_Source + 0x0A) = 0xFF;
                    *(Pointer_Raw_Source + 0x0B) = 0xE0;

                }
                else
                {
                    // 32-bit systems use 32-bit relative offset and jump
                    // 5 byte destructive

                    // Get function pointers
                    int Source_Base = source.MethodHandle.GetFunctionPointer().ToInt32();
                    int Destination_Base = target.MethodHandle.GetFunctionPointer().ToInt32();

                    // Native source address
                    byte* Pointer_Raw_Source = (byte*)Source_Base;

                    // Pointer to insert jump address into native code
                    int* Pointer_Raw_Address = (int*)(Pointer_Raw_Source + 1);

                    // Jump offset (less instruction size)
                    int offset = (Destination_Base - Source_Base) - 5;

                    // Insert 32-bit relative jump into native code
                    *Pointer_Raw_Source = 0xE9;
                    *Pointer_Raw_Address = offset;
                }


            }
        }
    }
}
