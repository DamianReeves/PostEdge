using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using PostSharp.Sdk.CodeModel;

namespace PostEdge.Weaver.Transformations {
    internal sealed class PropertyNotificationAssets {
        public readonly ITypeSignature INotifyPropertyChangedTypeSignature;
        public readonly ITypeSignature INotifyPropertyChangingTypeSignature;

        public readonly ITypeSignature PropertyChangedEventHandlerTypeSignature;
        public readonly ITypeSignature PropertyChangingEventHandlerTypeSignature;

        public readonly ITypeSignature PropertyChangedEventArgsTypeSignature;
        public readonly ITypeSignature PropertyChangingEventArgsTypeSignature;

        public readonly IMethod PropertyChangedEventArgsConstructor;
        public readonly IMethod PropertyChangingEventArgsConstructor;
        public readonly IMethod PropertyChangedEventHandlerInvokeMethod;
        public readonly IMethod PropertyChangingEventHandlerInvokeMethod;

        public PropertyNotificationAssets(ModuleDeclaration module) {
            if (module == null) throw new ArgumentNullException("module");
            Contract.EndContractBlock();
            //INotifyPropertyChanged Related
            INotifyPropertyChangedTypeSignature = module.FindType(typeof(INotifyPropertyChanged));
            PropertyChangedEventHandlerTypeSignature =
                module.FindType(typeof(INotifyPropertyChanged).GetEvent("PropertyChanged").EventHandlerType);

            PropertyChangedEventHandlerInvokeMethod =
                module.FindMethod(PropertyChangedEventHandlerTypeSignature, "Invoke");

            PropertyChangedEventArgsTypeSignature =
                module.FindType(typeof(PropertyChangedEventArgs));

            PropertyChangedEventArgsConstructor =
                module.FindMethod(PropertyChangedEventArgsTypeSignature, ".ctor");

            //INotifyPropertyChanging Realted
            INotifyPropertyChangingTypeSignature =
                module.FindType(typeof(INotifyPropertyChanging));
            PropertyChangingEventHandlerTypeSignature =
                module.FindType(typeof(INotifyPropertyChanging).GetEvent("PropertyChanging").EventHandlerType);

            PropertyChangingEventHandlerInvokeMethod =
                module.FindMethod(PropertyChangingEventHandlerTypeSignature, "Invoke");

            PropertyChangingEventArgsTypeSignature =
                module.FindType(typeof(PropertyChangingEventArgs));

            PropertyChangingEventArgsConstructor =
                module.FindMethod(PropertyChangingEventArgsTypeSignature, ".ctor");
        }
    }
}