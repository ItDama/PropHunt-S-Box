namespace Sandbox;
using Sandbox;

public sealed class PropInfo : Component
{
	/// <summary>
	/// Model renderer of the prop
	/// </summary>
	[Property]
	public ModelRenderer Model { get; set; }
	
	[Property] public BoxCollider Collider { get; set; }
	[Property] public Rigidbody Rigidbody { get; set; }
	
	protected override void OnUpdate()
	{

	}
}
