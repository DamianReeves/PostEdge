using System.ComponentModel.Composition;
using PostSharp.Aspects.Configuration;
using PostSharp.Extensibility;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.AspectWeaver.Transformations;

namespace PostEdge.Weaver.Internal {
    internal abstract class ComposedAdviceWeaver : AdviceWeaver {
        protected ComposedAdviceWeaver() {
            CompositionInitializer.SatisfyImports(this);    
        }
        
    }
    internal abstract class ComposedAspectWeaver : AspectWeaver {
        protected ComposedAspectWeaver(AspectConfigurationAttribute defaultConfiguration, ReflectionObjectBuilder reflectionObjectBuilder, MulticastTargets validTargets) 
            : base(defaultConfiguration, reflectionObjectBuilder, validTargets) {
                CompositionInitializer.SatisfyImports(this);
        } 
    }

    internal abstract class ComposedAdviceGroup:AdviceGroup{
        protected ComposedAdviceGroup(AdviceWeaver parent) : base(parent) {
            CompositionInitializer.SatisfyImports(this);
        }
    }

    internal abstract class ComposedAspectWeaverInstance<TAspectWeaver>:AspectWeaverInstance 
        where TAspectWeaver:ComposedAspectWeaver {
        protected ComposedAspectWeaverInstance(TAspectWeaver aspectWeaver, AspectInstanceInfo aspectInstanceInfo) 
            : base(aspectWeaver, aspectInstanceInfo) {
                CompositionInitializer.SatisfyImports(this);
        }

        public new TAspectWeaver AspectWeaver {
            get { return (TAspectWeaver) base.AspectWeaver; }
        }
    }

    internal abstract class ComposedMethodBodyTransformation : MethodBodyTransformation {
        protected ComposedMethodBodyTransformation(AspectWeaver aspectWeaver) 
            : base(aspectWeaver) {
                CompositionInitializer.SatisfyImports(this);
        }
    }

    internal abstract class ComposedMethodBodyTransformationInstance:MethodBodyTransformationInstance {
        protected ComposedMethodBodyTransformationInstance(MethodBodyTransformation parent, AspectWeaverInstance aspectWeaverInstance) 
            : base(parent, aspectWeaverInstance) {
            CompositionInitializer.SatisfyImports(this);
        }
    }

    internal abstract class ComposedStructuralTransformation: StructuralTransformation {
        protected ComposedStructuralTransformation(AspectWeaver aspectWeaver) 
            : base(aspectWeaver) {
            CompositionInitializer.SatisfyImports(this);
        }
    }

    internal abstract class ComposedStructuralTransformationInstance : StructuralTransformationInstance {
        protected ComposedStructuralTransformationInstance(StructuralTransformation parent, AspectWeaverInstance aspectWeaverInstance) 
            : base(parent, aspectWeaverInstance) {
            CompositionInitializer.SatisfyImports(this);
        }
    }
}