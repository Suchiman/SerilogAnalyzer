﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SerilogAnalyzer {
    using System;
    using System.Reflection;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SerilogAnalyzer.Resources", typeof(Resources).GetTypeInfo().Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Checks that MessageTemplates are constant values which is recommended practice.
        /// </summary>
        internal static string ConstantMessageTemplateAnalyzerDescription {
            get {
                return ResourceManager.GetString("ConstantMessageTemplateAnalyzerDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to MessageTemplate argument {0} is not constant.
        /// </summary>
        internal static string ConstantMessageTemplateAnalyzerMessageFormat {
            get {
                return ResourceManager.GetString("ConstantMessageTemplateAnalyzerMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Constant MessageTemplate verifier.
        /// </summary>
        internal static string ConstantMessageTemplateAnalyzerTitle {
            get {
                return ResourceManager.GetString("ConstantMessageTemplateAnalyzerTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Exceptions should be passed in the Exception Parameter.
        /// </summary>
        internal static string ExceptionAnalyzerDescription {
            get {
                return ResourceManager.GetString("ExceptionAnalyzerDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The exception &apos;{0}&apos; should be passed as first argument.
        /// </summary>
        internal static string ExceptionAnalyzerMessageFormat {
            get {
                return ResourceManager.GetString("ExceptionAnalyzerMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Exception not passed as first argument.
        /// </summary>
        internal static string ExceptionAnalyzerTitle {
            get {
                return ResourceManager.GetString("ExceptionAnalyzerTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Checks wether properties and arguments match up.
        /// </summary>
        internal static string PropertyBindingAnalyzerDescription {
            get {
                return ResourceManager.GetString("PropertyBindingAnalyzerDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error while binding properties: {0}.
        /// </summary>
        internal static string PropertyBindingAnalyzerMessageFormat {
            get {
                return ResourceManager.GetString("PropertyBindingAnalyzerMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Property binding verifier.
        /// </summary>
        internal static string PropertyBindingAnalyzerTitle {
            get {
                return ResourceManager.GetString("PropertyBindingAnalyzerTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Checks for errors in the MessageTemplate.
        /// </summary>
        internal static string TemplateAnalyzerDescription {
            get {
                return ResourceManager.GetString("TemplateAnalyzerDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error while parsing MessageTemplate: {0}.
        /// </summary>
        internal static string TemplateAnalyzerMessageFormat {
            get {
                return ResourceManager.GetString("TemplateAnalyzerMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to MessageTemplate verifier.
        /// </summary>
        internal static string TemplateAnalyzerTitle {
            get {
                return ResourceManager.GetString("TemplateAnalyzerTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Checks that all property names in a MessageTemplates are unique.
        /// </summary>
        internal static string UniquePropertyNameAnalyzerDescription {
            get {
                return ResourceManager.GetString("UniquePropertyNameAnalyzerDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Property name &apos;{0}&apos; is not unique in this MessageTemplate.
        /// </summary>
        internal static string UniquePropertyNameAnalyzerMessageFormat {
            get {
                return ResourceManager.GetString("UniquePropertyNameAnalyzerMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unique Property name verifier.
        /// </summary>
        internal static string UniquePropertyNameAnalyzerTitle {
            get {
                return ResourceManager.GetString("UniquePropertyNameAnalyzerTitle", resourceCulture);
            }
        }
    }
}