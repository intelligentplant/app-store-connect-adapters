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
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;Anonymous&gt;.
        /// </summary>
        internal static string AdapterSubscription_AnonymousUserName {
            get {
                return ResourceManager.GetString("AdapterSubscription_AnonymousUserName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Maximum adapter description length is {0}..
        /// </summary>
        internal static string Error_AdapterDescriptionIsTooLong {
            get {
                return ResourceManager.GetString("Error_AdapterDescriptionIsTooLong", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Maximum adapter ID length is {0}..
        /// </summary>
        internal static string Error_AdapterIdIsTooLong {
            get {
                return ResourceManager.GetString("Error_AdapterIdIsTooLong", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An adapter cannot be started when it is already running..
        /// </summary>
        internal static string Error_AdapterIsAlreadyStarted {
            get {
                return ResourceManager.GetString("Error_AdapterIsAlreadyStarted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The adapter is disabled..
        /// </summary>
        internal static string Error_AdapterIsDisabled {
            get {
                return ResourceManager.GetString("Error_AdapterIsDisabled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Adapter &apos;{0}&apos; is not compatible with &apos;{1}&apos;..
        /// </summary>
        internal static string Error_AdapterIsNotCompatibleWithHelperClass {
            get {
                return ResourceManager.GetString("Error_AdapterIsNotCompatibleWithHelperClass", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The adapter has not been started..
        /// </summary>
        internal static string Error_AdapterIsNotStarted {
            get {
                return ResourceManager.GetString("Error_AdapterIsNotStarted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The adapter is currently stopping and cannot be started until this operation completes..
        /// </summary>
        internal static string Error_AdapterIsStopping {
            get {
                return ResourceManager.GetString("Error_AdapterIsStopping", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Maximum adapter name length is {0}..
        /// </summary>
        internal static string Error_AdapterNameIsTooLong {
            get {
                return ResourceManager.GetString("Error_AdapterNameIsTooLong", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The bucket size for plot calculations must be greater than zero..
        /// </summary>
        internal static string Error_BucketSizeMustBeGreaterThanZero {
            get {
                return ResourceManager.GetString("Error_BucketSizeMustBeGreaterThanZero", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to resolve any requested subscription topics..
        /// </summary>
        internal static string Error_CannotResolveAnySubscriptionTopics {
            get {
                return ResourceManager.GetString("Error_CannotResolveAnySubscriptionTopics", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This feature has already been registered..
        /// </summary>
        internal static string Error_FeatureIsAlreadyRegistered {
            get {
                return ResourceManager.GetString("Error_FeatureIsAlreadyRegistered", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Feature is unavailable..
        /// </summary>
        internal static string Error_FeatureUnavailable {
            get {
                return ResourceManager.GetString("Error_FeatureUnavailable", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid base date: {0}.
        /// </summary>
        internal static string Error_InvalidBaseDate {
            get {
                return ResourceManager.GetString("Error_InvalidBaseDate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The specified URI is not a valid extension feature operation URI..
        /// </summary>
        internal static string Error_InvalidExtensionFeatureOperationUri {
            get {
                return ResourceManager.GetString("Error_InvalidExtensionFeatureOperationUri", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Object does not implement feature &apos;{0}&apos;..
        /// </summary>
        internal static string Error_NotAFeatureImplementation {
            get {
                return ResourceManager.GetString("Error_NotAFeatureImplementation", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;{0}&apos; is not a valid adapter feature type. Standard features must be interfaces that extend &apos;{1}&apos; and are annotated with &apos;{2}&apos;. Non-standard features must be interfaces or classes that extend &apos;{3}&apos; and are annotated with &apos;{4}&apos;..
        /// </summary>
        internal static string Error_NotAnAdapterFeature {
            get {
                return ResourceManager.GetString("Error_NotAnAdapterFeature", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You must specify an extension feature interface type (i.e. an interface derived from {0} and annotated with {1})..
        /// </summary>
        internal static string Error_NotAnExtensionFeatureInterface {
            get {
                return ResourceManager.GetString("Error_NotAnExtensionFeatureInterface", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Too many subscriptions..
        /// </summary>
        internal static string Error_TooManySubscriptions {
            get {
                return ResourceManager.GetString("Error_TooManySubscriptions", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The adapter is running with degraded health status..
        /// </summary>
        internal static string HealthChecks_CompositeResultDescription_Degraded {
            get {
                return ResourceManager.GetString("HealthChecks_CompositeResultDescription_Degraded", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error while performing adapter health checks..
        /// </summary>
        internal static string HealthChecks_CompositeResultDescription_Error {
            get {
                return ResourceManager.GetString("HealthChecks_CompositeResultDescription_Error", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The adapter is running with healthy status..
        /// </summary>
        internal static string HealthChecks_CompositeResultDescription_Healthy {
            get {
                return ResourceManager.GetString("HealthChecks_CompositeResultDescription_Healthy", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The adapter is not running..
        /// </summary>
        internal static string HealthChecks_CompositeResultDescription_NotStarted {
            get {
                return ResourceManager.GetString("HealthChecks_CompositeResultDescription_NotStarted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The adapter is running with unhealthy status..
        /// </summary>
        internal static string HealthChecks_CompositeResultDescription_Unhealthy {
            get {
                return ResourceManager.GetString("HealthChecks_CompositeResultDescription_Unhealthy", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Active Subscriber Count.
        /// </summary>
        internal static string HealthChecks_Data_ActiveSubscriberCount {
            get {
                return ResourceManager.GetString("HealthChecks_Data_ActiveSubscriberCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Connection ID.
        /// </summary>
        internal static string HealthChecks_Data_ConnectionId {
            get {
                return ResourceManager.GetString("HealthChecks_Data_ConnectionId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Correlation ID.
        /// </summary>
        internal static string HealthChecks_Data_CorrelationId {
            get {
                return ResourceManager.GetString("HealthChecks_Data_CorrelationId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Passive Subscriber Count.
        /// </summary>
        internal static string HealthChecks_Data_PassiveSubscriberCount {
            get {
                return ResourceManager.GetString("HealthChecks_Data_PassiveSubscriberCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Subscriber Count.
        /// </summary>
        internal static string HealthChecks_Data_SubscriberCount {
            get {
                return ResourceManager.GetString("HealthChecks_Data_SubscriberCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Tag Count.
        /// </summary>
        internal static string HealthChecks_Data_TagCount {
            get {
                return ResourceManager.GetString("HealthChecks_Data_TagCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Topic Count.
        /// </summary>
        internal static string HealthChecks_Data_TopicCount {
            get {
                return ResourceManager.GetString("HealthChecks_Data_TopicCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to User.
        /// </summary>
        internal static string HealthChecks_Data_UserName {
            get {
                return ResourceManager.GetString("HealthChecks_Data_UserName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Last Emit Time.
        /// </summary>
        internal static string HealthChecks_Data_UtcLastEmit {
            get {
                return ResourceManager.GetString("HealthChecks_Data_UtcLastEmit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Feature Health: {0}.
        /// </summary>
        internal static string HealthChecks_DisplayName_FeatureHealth {
            get {
                return ResourceManager.GetString("HealthChecks_DisplayName_FeatureHealth", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Adapter Health.
        /// </summary>
        internal static string HealthChecks_DisplayName_OverallAdapterHealth {
            get {
                return ResourceManager.GetString("HealthChecks_DisplayName_OverallAdapterHealth", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while starting adapter &apos;{AdapterId}&apos;..
        /// </summary>
        internal static string Log_AdapterStartupError {
            get {
                return ResourceManager.GetString("Log_AdapterStartupError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while stopping adapter &apos;{AdapterId}&apos;..
        /// </summary>
        internal static string Log_AdapterStopError {
            get {
                return ResourceManager.GetString("Log_AdapterStopError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Stopping adapter &apos;{AdapterId}&apos; (disposing: true)..
        /// </summary>
        internal static string Log_DisposingAdapter {
            get {
                return ResourceManager.GetString("Log_DisposingAdapter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error while refreshing subscribed snapshot tag values..
        /// </summary>
        internal static string Log_ErrorDuringSnapshotPushRefresh {
            get {
                return ResourceManager.GetString("Log_ErrorDuringSnapshotPushRefresh", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error in background task &apos;{WorkItem}&apos;..
        /// </summary>
        internal static string Log_ErrorInBackgroundTask {
            get {
                return ResourceManager.GetString("Log_ErrorInBackgroundTask", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred in the event subscription manager publish loop..
        /// </summary>
        internal static string Log_ErrorInEventSubscriptionManagerPublishLoop {
            get {
                return ResourceManager.GetString("Log_ErrorInEventSubscriptionManagerPublishLoop", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while the snapshot subscription manager was polling for new values..
        /// </summary>
        internal static string Log_ErrorInSnapshotPollingUpdateLoop {
            get {
                return ResourceManager.GetString("Log_ErrorInSnapshotPollingUpdateLoop", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred in the snapshot subscription manager publish loop..
        /// </summary>
        internal static string Log_ErrorInSnapshotSubscriptionManagerPublishLoop {
            get {
                return ResourceManager.GetString("Log_ErrorInSnapshotSubscriptionManagerPublishLoop", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while disposing of an event message subscription..
        /// </summary>
        internal static string Log_ErrorWhileDisposingOfEventMessageSubscription {
            get {
                return ResourceManager.GetString("Log_ErrorWhileDisposingOfEventMessageSubscription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while disposing of a snapshot subscription..
        /// </summary>
        internal static string Log_ErrorWhileDisposingOfSnapshotSubscription {
            get {
                return ResourceManager.GetString("Log_ErrorWhileDisposingOfSnapshotSubscription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while processing a subscription change: Tag = &apos;{tag}&apos;, Action = &apos;{action}&apos;.
        /// </summary>
        internal static string Log_ErrorWhileProcessingSnapshotSubscriptionChange {
            get {
                return ResourceManager.GetString("Log_ErrorWhileProcessingSnapshotSubscriptionChange", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while processing a subscription change: Topic = &apos;{topic}&apos;, Action = &apos;{action}&apos;.
        /// </summary>
        internal static string Log_ErrorWhileProcessingSubscriptionTopicChange {
            get {
                return ResourceManager.GetString("Log_ErrorWhileProcessingSubscriptionTopicChange", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Evicted event message &apos;{id}&apos; with cursor position &apos;{cursorPosition}&apos; and timestamp &apos;{timestamp}&apos;..
        /// </summary>
        internal static string Log_InMemoryEventMessageManager_EvictedMessage {
            get {
                return ResourceManager.GetString("Log_InMemoryEventMessageManager_EvictedMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Wrote event message &apos;{id}&apos; with cursor position &apos;{cursorPosition}&apos; and timestamp &apos;{timestamp}&apos;..
        /// </summary>
        internal static string Log_InMemoryEventMessageManager_WroteMessage {
            get {
                return ResourceManager.GetString("Log_InMemoryEventMessageManager_WroteMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Updated adapter options are not valid..
        /// </summary>
        internal static string Log_InvalidAdapterOptionsUpdate {
            get {
                return ResourceManager.GetString("Log_InvalidAdapterOptionsUpdate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Publish to subscriber with connection ID &apos;{connectionId}&apos; failed with error..
        /// </summary>
        internal static string Log_PublishToSubscriberThrewException {
            get {
                return ResourceManager.GetString("Log_PublishToSubscriberThrewException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Publish to subscriber with connection ID &apos;{connectionId}&apos; failed..
        /// </summary>
        internal static string Log_PublishToSubscriberWasUnsuccessful {
            get {
                return ResourceManager.GetString("Log_PublishToSubscriberWasUnsuccessful", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Started adapter &apos;{AdapterId}&apos;..
        /// </summary>
        internal static string Log_StartedAdapter {
            get {
                return ResourceManager.GetString("Log_StartedAdapter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Starting adapter &apos;{AdapterId}&apos;..
        /// </summary>
        internal static string Log_StartingAdapter {
            get {
                return ResourceManager.GetString("Log_StartingAdapter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Stopped adapter &apos;{AdapterId}&apos;..
        /// </summary>
        internal static string Log_StoppedAdapter {
            get {
                return ResourceManager.GetString("Log_StoppedAdapter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Stopping adapter &apos;{AdapterId}&apos; (disposing: false)..
        /// </summary>
        internal static string Log_StoppingAdapter {
            get {
                return ResourceManager.GetString("Log_StoppingAdapter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ERROR.
        /// </summary>
        internal static string TagValue_ProcessedValue_Error {
            get {
                return ResourceManager.GetString("TagValue_ProcessedValue_Error", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No data available in the bucket..
        /// </summary>
        internal static string TagValue_ProcessedValue_NoData {
            get {
                return ResourceManager.GetString("TagValue_ProcessedValue_NoData", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No good-quality data available in the bucket..
        /// </summary>
        internal static string TagValue_ProcessedValue_NoGoodData {
            get {
                return ResourceManager.GetString("TagValue_ProcessedValue_NoGoodData", resourceCulture);
            }
        }
    }
}
