namespace PostEdge.Aspects {
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    using PostEdge.Aspects.Configuration;
    using PostEdge.Aspects.Dependencies;

    using PostSharp.Aspects;
    using PostSharp.Aspects.Configuration;
    using PostSharp.Aspects.Dependencies;
    using PostSharp.Extensibility;

    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module |
                 AttributeTargets.Class | AttributeTargets.Struct | 
                 AttributeTargets.Interface | AttributeTargets.Property,
                 AllowMultiple = true, Inherited = true)]
    [MulticastAttributeUsage(MulticastTargets.Property, AllowMultiple = false)]
    [ProvideAspectRole(PostEdgeStandardRoles.ChangeTracking)]
    [RequirePostSharp("PostEdge", "PostEdge")]
    [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.Before, PostEdgeStandardRoles.NotifyPropertyChanged)]
    [Serializable]
    public class ChangeTrackingAttribute : MulticastAttribute, IPostEdgeAspect, IAspectProvider, IAspectBuildSemantics {
        private readonly bool _enable = true;
        private readonly bool _performEqualityCheck = true;
        public ChangeTrackingAttribute() {
            this.AttributePriority = 100;
        }
        public ChangeTrackingAttribute(bool enable) {
            this._enable = enable;
            this.AttributePriority = 100;
        }
        public ChangeTrackingAttribute(bool enable, bool performEqualityCheck) {
            this._enable = enable;
            this._performEqualityCheck = performEqualityCheck;
            this.AttributePriority = 100;
        }

        public bool Enable {
            get { return this._enable; }
        }

        public bool PerformEqualityCheck {
            get { return this._performEqualityCheck; }
        }

        /// <summary>
        /// Provides new aspects.
        /// </summary>
        /// <param name="targetElement">Code element (<see cref="T:System.Runtime.InteropServices._Assembly"/>, <see cref="T:System.Type"/>, 
        ///             <see cref="T:System.Reflection.FieldInfo"/>, <see cref="T:System.Reflection.MethodBase"/>, <see cref="T:System.Reflection.PropertyInfo"/>, <see cref="T:System.Reflection.EventInfo"/>, 
        ///             <see cref="T:System.Reflection.ParameterInfo"/>, or <see cref="T:PostSharp.Reflection.LocationInfo"/>) to which the current aspect has been applied.
        ///             </param>
        /// <returns>
        /// A set of aspect instances.
        /// </returns>
        public IEnumerable<AspectInstance> ProvideAspects(object targetElement) {
            if (this.PerformEqualityCheck) {
                yield return new AspectInstance(targetElement, new GuardPropertyEqualityAspect());
            }
        }

        /// <summary>
        /// Method invoked at build time to ensure that the aspect has been applied to
        ///             the right target.
        /// </summary>
        /// <param name="target">Target element.</param>
        /// <returns>
        /// <c>true</c> if the aspect was applied to an acceptable target, otherwise
        ///             <c>false</c>.
        /// </returns>
        public bool CompileTimeValidate(object target) {
            var property = target as PropertyInfo;
            if (property == null) {
                var memberInfo = target as MemberInfo;
                if (memberInfo != null) {
                    //AspectMessageSource.Error("PE0007", 
                    //    memberInfo.Name, 
                    //    memberInfo.DeclaringType == null
                    //        ? "<Unknown>" 
                    //        : memberInfo.DeclaringType.AssemblyQualifiedName
                    //);
                }
                return false;
            }

            //The property needs to be writeable
            if (!property.CanWrite) return false;
            return true;
        }

        /// <summary>
        /// Method invoked at build time to get the imperative configuration of the current <see cref="T:PostSharp.Aspects.Aspect"/>.
        /// </summary>
        /// <param name="targetElement">Code element (<see cref="T:System.Runtime.InteropServices._Assembly"/>, <see cref="T:System.Type"/>, 
        ///             <see cref="T:System.Reflection.FieldInfo"/>, <see cref="T:System.Reflection.MethodBase"/>, <see cref="T:System.Reflection.PropertyInfo"/>, <see cref="T:System.Reflection.EventInfo"/>, 
        ///             <see cref="T:System.Reflection.ParameterInfo"/>, or <see cref="T:PostSharp.Reflection.LocationInfo"/>) to which the current aspect has been applied.
        ///             </param>
        /// <returns>
        /// An <see cref="T:PostSharp.Aspects.Configuration.AspectConfiguration"/> representing the imperative configuration
        ///             of the current <see cref="T:PostSharp.Aspects.Aspect"/>.
        /// </returns>
        public AspectConfiguration GetAspectConfiguration(object targetElement) {
            return new ChangeTrackingAspectConfiguration {
                PerformEqualityCheck = this.PerformEqualityCheck,
                Enable = this.Enable,
            };
        }
    }
}
