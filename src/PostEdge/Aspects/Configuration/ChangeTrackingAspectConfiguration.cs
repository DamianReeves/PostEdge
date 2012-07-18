namespace PostEdge.Aspects.Configuration {
    using PostSharp.Aspects.Configuration;

    public class ChangeTrackingAspectConfiguration: AspectConfiguration {
        
        public bool Enable { get; set; }
        public bool PerformEqualityCheck { get; set; }
    }

    public sealed class ChangeTackingAspectConfigurationAttribute: AspectConfigurationAttribute {
        protected override AspectConfiguration CreateAspectConfiguration() {
            return new ChangeTrackingAspectConfiguration();
        }
    }
}