using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using Forms = System.Windows.Forms;

namespace CSharpSandbox.Wpf.View.WinForms
{
    public class RichTextBox5 : Forms.RichTextBox
    {
        private static IntPtr moduleHandle;

        protected override Forms.CreateParams CreateParams
        {
            get
            {
                if (moduleHandle == IntPtr.Zero)
                {
                    moduleHandle = LoadLibrary("msftedit.dll");
                    if ((long)moduleHandle < 0x20) throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not load Msftedit.dll");
                }
                Forms.CreateParams createParams = base.CreateParams;
                createParams.ClassName = "RichEdit50W";
                if (Multiline)
                {
                    if (((ScrollBars & Forms.RichTextBoxScrollBars.Horizontal) != Forms.RichTextBoxScrollBars.None) && !WordWrap)
                    {
                        createParams.Style |= 0x100000;
                        if ((ScrollBars & ((Forms.RichTextBoxScrollBars)0x10)) != Forms.RichTextBoxScrollBars.None)
                        {
                            createParams.Style |= 0x2000;
                        }
                    }
                    if ((ScrollBars & Forms.RichTextBoxScrollBars.Vertical) != Forms.RichTextBoxScrollBars.None)
                    {
                        createParams.Style |= 0x200000;
                        if ((ScrollBars & ((Forms.RichTextBoxScrollBars)0x10)) != Forms.RichTextBoxScrollBars.None)
                        {
                            createParams.Style |= 0x2000;
                        }
                    }
                }
                if ((Forms.BorderStyle.FixedSingle == BorderStyle) && ((createParams.Style & 0x800000) != 0))
                {
                    createParams.Style &= -8388609;
                    createParams.ExStyle |= 0x200;
                }
                return createParams;
            }
        }

        // P/Invoke declarations
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string path);

    }
}
