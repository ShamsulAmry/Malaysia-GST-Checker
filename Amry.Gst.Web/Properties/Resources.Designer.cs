﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Amry.Gst.Web.Properties {
    using System;
    
    
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Amry.Gst.Web.Properties.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to The given data is not live and cannot be cached..
        /// </summary>
        internal static string CannotCacheStaleData {
            get {
                return ResourceManager.GetString("CannotCacheStaleData", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Customs&apos; server returned an error.
        /// </summary>
        internal static string CustomsGstExceptionHttpResponseReasonPhrase {
            get {
                return ResourceManager.GetString("CustomsGstExceptionHttpResponseReasonPhrase", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot proceed with the query request.
        /// </summary>
        internal static string InternalGstExceptionHttpResponseReasonPhrase {
            get {
                return ResourceManager.GetString("InternalGstExceptionHttpResponseReasonPhrase", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Over 100 Results Returned. Please Narrow Search..
        /// </summary>
        internal static string Over100Results {
            get {
                return ResourceManager.GetString("Over100Results", resourceCulture);
            }
        }
    }
}
