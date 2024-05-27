using Sandbox;

public sealed class Snot : Component
{
	[Property] public UnitInfo Info { get; set; }
	
	[Property] public SkinnedModelRenderer Model { get; set; }
	
	

	protected override void OnUpdate()
	{
		if ( Model != null )
		{
			Model.Set( "hit",true );
			Model.Set("damage", 5f);
		}
	}
}
