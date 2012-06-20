using System;
using System.Diagnostics;
using PostSharp.Sdk.CodeModel;

namespace PostEdge.Weaver {
    public class PostEdgeWeavingHelper {
        private readonly ModuleDeclaration _module;
        private readonly Lazy<IMethod> _delegateCombineMethod;
        private readonly Lazy<IMethod> _delegateRemoveMethod;
        private readonly Lazy<IMethod> _compareExchangeMethod;

        public PostEdgeWeavingHelper(ModuleDeclaration module) {
            if (module == null) throw new ArgumentNullException("module");
            _module = module;
            _delegateCombineMethod =
                new Lazy<IMethod>(() => {
                    var method = _module.FindMethod("System.Delegate, mscorlib", "Combine", "System.Delegate, mscorlib","System.Delegate, mscorlib");
                    Debug.Assert(method != null, "Could not find Delegate.Combine used by PostEdgeWeavingHelper.");
                    return method;
                });

            _delegateRemoveMethod =
                new Lazy<IMethod>(() => {
                    var method = _module.FindMethod("System.Delegate, mscorlib", "Remove", "System.Delegate, mscorlib", "System.Delegate, mscorlib");
                    Debug.Assert(method != null, "Could not find Delegate.Remove used by PostEdgeWeavingHelper.");
                    return method;
                });

            _compareExchangeMethod =
                new Lazy<IMethod>(() => {
                    var method = _module.FindMethod("System.Threading.Interlocked, mscorlib", "CompareExchange", 
                        methodDef => methodDef.IsGenericDefinition);
                    Debug.Assert(method != null, "System.Threading.Interlocked.CompareExchange used by PostEdgeWeavingHelper.");
                    return method;
                });
        }

        public ModuleDeclaration Module {get { return _module; }}

        public IMethod DelegateCombineMethod {
            get { return _delegateCombineMethod.Value; }
        }

        public IMethod DelegateRemoveMethod {
            get { return _delegateRemoveMethod.Value; }
        }

        public IMethod CompareExchangeMethod {
            get { return _compareExchangeMethod.Value; }
        }
    }
}