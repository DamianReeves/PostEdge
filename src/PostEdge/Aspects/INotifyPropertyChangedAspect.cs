using PostSharp.Aspects;
using PostSharp.Extensibility;

namespace PostEdge.Aspects
{
    public interface INotifyPropertyChangedAspect: IInstanceScopedAspect, IPostEdgeAspect {
        
    }

    [RequirePostSharp("PostEdge","PostEdge")]
    public interface IPostEdgeAspect: IAspect{}
}