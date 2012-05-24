using System.Collections.Generic;
using System.Linq;
using System.Text;
using PostEdge.Aspects;
using PostEdge.Aspects.Advices;
using PostSharp.Sdk.AspectWeaver;

namespace PostEdge.Weaver {
    public class PostEdgePlugIn: AspectWeaverPlugIn {
        public PostEdgePlugIn() : base(StandardPriorities.User) {}        

        protected override void Initialize() {
            base.Initialize();
            this.BindAdviceWeaver<SetPropertyAdvice, SetPropertyAdviceWeaver>();
            this.BindAdviceWeaver<EnhancePropertySetterAttribute, EnhancePropertySetterAdviceWeaver>();
            this.BindAspectWeaver<IGuardPropertyEqualityAspect,GuardPropertyEqualityAspectWeaver>();
        }
    }
}
