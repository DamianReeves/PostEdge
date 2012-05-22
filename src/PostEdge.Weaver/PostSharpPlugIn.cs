using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PostSharp.Sdk.AspectWeaver;

namespace PostEdge.Weaver {
    public class PostSharpPlugIn: AspectWeaverPlugIn {
        public PostSharpPlugIn() : base(StandardPriorities.User) {}        

        protected override void Initialize() {
            base.Initialize();
        }
    }
}
