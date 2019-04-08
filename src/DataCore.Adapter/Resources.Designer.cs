﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DataCore.Adapter {
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
    public class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("DataCore.Adapter.Resources", typeof(Resources).Assembly);
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
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Average value calculated over a fixed sample interval..
        /// </summary>
        public static string DataFunction_Avg_Description {
            get {
                return ResourceManager.GetString("DataFunction_Avg_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Maximum value calculated over a fixed sample interval..
        /// </summary>
        public static string DataFunction_Max_Description {
            get {
                return ResourceManager.GetString("DataFunction_Max_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Minimum value calculated over a fixed sample interval..
        /// </summary>
        public static string DataFunction_Min_Description {
            get {
                return ResourceManager.GetString("DataFunction_Min_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You must specify an adapter ID..
        /// </summary>
        public static string Error_AdapterDescriptorIdIsRequired {
            get {
                return ResourceManager.GetString("Error_AdapterDescriptorIdIsRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You must specify an adapter name..
        /// </summary>
        public static string Error_AdapterDescriptorNameIsRequired {
            get {
                return ResourceManager.GetString("Error_AdapterDescriptorNameIsRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You cannot specify a null data function name..
        /// </summary>
        public static string Error_DataFunctionCannotBeNull {
            get {
                return ResourceManager.GetString("Error_DataFunctionCannotBeNull", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You cannot specify multiple write collections for the same tag..
        /// </summary>
        public static string Error_DuplicateTagWriteCollectionsAreNotAllowed {
            get {
                return ResourceManager.GetString("Error_DuplicateTagWriteCollectionsAreNotAllowed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to When interpolating or inferring a tag value using raw samples, you must specify a values to base the calculation on..
        /// </summary>
        public static string Error_InterpolationRequiresAtLeastOneSample {
            get {
                return ResourceManager.GetString("Error_InterpolationRequiresAtLeastOneSample", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to When interpolating or inferring a tag value using raw samples, you must specify a sample that is earlier than the sample time you want to calculate at..
        /// </summary>
        public static string Error_InterpolationRequiresAtLeastOneSampleEarlierThanRequestedSampleTime {
            get {
                return ResourceManager.GetString("Error_InterpolationRequiresAtLeastOneSampleEarlierThanRequestedSampleTime", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Interval count must be greater than zero..
        /// </summary>
        public static string Error_IntervalCountMustBeGreaterThanZero {
            get {
                return ResourceManager.GetString("Error_IntervalCountMustBeGreaterThanZero", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid time span..
        /// </summary>
        public static string Error_InvalidTimeSpan {
            get {
                return ResourceManager.GetString("Error_InvalidTimeSpan", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid time stamp..
        /// </summary>
        public static string Error_InvalidTimeStamp {
            get {
                return ResourceManager.GetString("Error_InvalidTimeStamp", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Adapter features must be interfaces extending {0}..
        /// </summary>
        public static string Error_NotAnAdapterFeature {
            get {
                return ResourceManager.GetString("Error_NotAnAdapterFeature", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Sample interval must be greater than zero..
        /// </summary>
        public static string Error_SampleIntervalMustBeGreaterThanZero {
            get {
                return ResourceManager.GetString("Error_SampleIntervalMustBeGreaterThanZero", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Start time cannot be greater than end time..
        /// </summary>
        public static string Error_StartTimeCannotBeGreaterThanEndTime {
            get {
                return ResourceManager.GetString("Error_StartTimeCannotBeGreaterThanEndTime", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Start time cannot be greater than or equal to end time..
        /// </summary>
        public static string Error_StartTimeCannotBeGreaterThanOrEqualToEndTime {
            get {
                return ResourceManager.GetString("Error_StartTimeCannotBeGreaterThanOrEqualToEndTime", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You cannot specify a null tag name or ID..
        /// </summary>
        public static string Error_TagNameOrIdCannotBeNull {
            get {
                return ResourceManager.GetString("Error_TagNameOrIdCannotBeNull", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to At least one tag search filter field must be specified..
        /// </summary>
        public static string Error_TagSearchRequiresAtLeastOneFilter {
            get {
                return ResourceManager.GetString("Error_TagSearchRequiresAtLeastOneFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Host information has not been provided..
        /// </summary>
        public static string HostInfo_Unspecified_Description {
            get {
                return ResourceManager.GetString("HostInfo_Unspecified_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unspecified.
        /// </summary>
        public static string HostInfo_Unspecified_Name {
            get {
                return ResourceManager.GetString("HostInfo_Unspecified_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No snapshot value was provided..
        /// </summary>
        public static string SnapshotTagValue_Unspecified_ErrorText {
            get {
                return ResourceManager.GetString("SnapshotTagValue_Unspecified_ErrorText", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unspecified.
        /// </summary>
        public static string SnapshotTagValue_Unspecified_Value {
            get {
                return ResourceManager.GetString("SnapshotTagValue_Unspecified_Value", resourceCulture);
            }
        }
    }
}
