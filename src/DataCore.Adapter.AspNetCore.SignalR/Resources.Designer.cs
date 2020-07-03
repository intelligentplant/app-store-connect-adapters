﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DataCore.Adapter.AspNetCore {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("DataCore.Adapter.AspNetCore.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to An {0} service is required. Did you forget to call {1}.{2}?.
        /// </summary>
        internal static string Error_AdapterAccessorIsRequired {
            get {
                return ResourceManager.GetString("Error_AdapterAccessorIsRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An existing subscription for this adapter could not be found..
        /// </summary>
        internal static string Error_AdapterSubscriptionDoesNotExist {
            get {
                return ResourceManager.GetString("Error_AdapterSubscriptionDoesNotExist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You must specify at least one tag to subscribe to..
        /// </summary>
        internal static string Error_AtLeastOneTagIsRequired {
            get {
                return ResourceManager.GetString("Error_AtLeastOneTagIsRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to resolve adapter ID: {0}.
        /// </summary>
        internal static string Error_CannotResolveAdapterId {
            get {
                return ResourceManager.GetString("Error_CannotResolveAdapterId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An event topic name is required..
        /// </summary>
        internal static string Error_EventTopicNameRequired {
            get {
                return ResourceManager.GetString("Error_EventTopicNameRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You are not authorized to access this feature..
        /// </summary>
        internal static string Error_NotAuthorizedToAccessFeature {
            get {
                return ResourceManager.GetString("Error_NotAuthorizedToAccessFeature", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The specified subscription does not exist..
        /// </summary>
        internal static string Error_SubscriptionDoesNotExist {
            get {
                return ResourceManager.GetString("Error_SubscriptionDoesNotExist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A subscription ID is required..
        /// </summary>
        internal static string Error_SubscriptionIdRequired {
            get {
                return ResourceManager.GetString("Error_SubscriptionIdRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A tag name or ID is required..
        /// </summary>
        internal static string Error_TagNameOrIdRequired {
            get {
                return ResourceManager.GetString("Error_TagNameOrIdRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unsupported interface: {0}.
        /// </summary>
        internal static string Error_UnsupportedInterface {
            get {
                return ResourceManager.GetString("Error_UnsupportedInterface", resourceCulture);
            }
        }
    }
}
