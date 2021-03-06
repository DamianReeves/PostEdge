using System;
using System.Reflection;
using PostEdge.Weaver.Extensions;
using PostSharp.Sdk.AspectInfrastructure;
using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.AspectWeaver.Transformations;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeWeaver;

namespace PostEdge.Weaver.Transformations {
    public abstract class MemberIntroductionTransformation:StructuralTransformation {
        protected MemberIntroductionTransformation(AspectWeaver aspectWeaver) : base(aspectWeaver) {
        }

        public WeavingHelper WeavingHelper { get { return AspectInfrastructureTask.WeavingHelper; } }  
    
        protected void IntroduceEvent(TransformationContext context, TypeDefDeclaration type, EventDeclaration theEvent) {
            var fieldRef = type.FindField(theEvent.Name);
            FieldDefDeclaration field;
            if(fieldRef == null) {
                field = new FieldDefDeclaration {
                    Attributes = FieldAttributes.Private,
                    Name = theEvent.Name,
                    FieldType = theEvent.EventType,
                };
                type.Fields.Add(field);
            } else {
                field = fieldRef.Field;
            }
            var addOn = theEvent.GetAccessor(MethodSemantics.AddOn);
            if (addOn == null) {
                theEvent.ImplementAddOn(type, field, AspectInfrastructureTask.WeavingHelper);
            }
            var removeOn = theEvent.GetAccessor(MethodSemantics.RemoveOn);
            if (removeOn == null) {
                theEvent.ImplementRemoveOn(type, field, AspectInfrastructureTask.WeavingHelper);
            }
        }
    }

    internal sealed class MethodIntroductionOptions {
        public MethodAttributes Attributes { get; set; }
        public bool IsVirtual { get; set; }
    }
}