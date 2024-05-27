using System;
using Sandbox;
using Sandbox.Citizen;

public sealed class PropHuntPlayer : Component
{
	[Property]
	[Category("Components")]
	public GameObject Camera { get; set; }
	
	[Property]
	[Category("Components")]
	public CharacterController Controller { get; set; }
	
	[Property]
	[Category("Components")]
	public CitizenAnimationHelper Animator { get; set; }
	
	[Property]
	[Category("Components")]
	public GameObject ModelRenderer { get; set; }
	
	/// <summary>
	/// How fast you can walk (units per second)
	/// </summary>
	[Property]
	[Category("Stats")]
	[Range(0f, 400f, 1f)]
	public float WalkSpeed { get; set; } = 130f;
	
	/// <summary>
	/// How fast you can run (units per second)
	/// </summary>
	[Property]
	[Category("Stats")]
	[Range(0f, 800f, 1f)]
	public float RunSpeed { get; set; } = 250.0f;
	
	/// <summary>
	/// How strong you can jump (units per second)
	/// </summary>
	[Property] 
	[Category("Stats")]
	[Range(0f, 1000f, 10f)]
	public float JumpStrength { get; set; } = 400.0f;
	
	/// <summary>
	/// How much damage your punch deals
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 5f, 0.1f )]
	public float PunchStrength { get; set; } = 1f;

	/// <summary>
	/// How many seconds before you can punch again
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 2f, 0.1f )]
	public float PunchCooldown { get; set; } = 0.5f;

	/// <summary>
	/// How far away you can punch in Hammer Units
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 200f, 5f )]
	public float PunchRange { get; set; } = 50f;
	
	/// <summary>
	/// Where the camera rotates around and the aim originates from 
	/// </summary>
	[Property]
	public Vector3 EyePosition { get; set; }
	
	public Angles EyeAngles { get; set; }

	public Vector3 EyeWorldPosition => Transform.Local.PointToWorld( EyePosition );
	
	Transform _initialCameraTransform;
	TimeSince _lastPunch;
	protected override void DrawGizmos()
	{
		if ( !Gizmo.IsSelected ) return;
		var draw = Gizmo.Draw;
		draw.LineSphere(EyePosition, 10f );
		draw.LineCylinder( EyePosition, EyePosition + Transform.Rotation.Forward * PunchRange,5f,5f ,10 );
	}

	protected override void OnUpdate()
	{
		
			EyeAngles += Input.AnalogLook;
			EyeAngles = EyeAngles.WithPitch( MathX.Clamp( EyeAngles.pitch, -60f, 60f ) );
			if ( Camera != null )
			{
				var cameraTransform = _initialCameraTransform.RotateAround( EyePosition, EyeAngles.WithYaw( 0f ) );
				var cameraPosition = Transform.Local.PointToWorld( cameraTransform.Position );
				var cameraTrace = Scene.Trace.Ray( EyeWorldPosition, cameraPosition )
					.Size( 3f )
					.IgnoreGameObject( GameObject )
					.WithoutTags( "player" )
					.Run();
				Camera.Transform.Position = cameraTrace.EndPosition;
				Camera.Transform.LocalRotation = cameraTransform.Rotation;
			}
		
		


	}


	
	
	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		
			Transform.Rotation = Rotation.FromYaw( EyeAngles.yaw );
		


		if(Controller == null) return;

		var wishSpeed = Input.Down( "Run" ) ? RunSpeed : WalkSpeed;
		var wishVelocity = Input.AnalogMove.Normal * wishSpeed * Transform.Rotation;
		
		Controller.Accelerate(wishVelocity);

		if ( Controller.IsOnGround )
		{
			Controller.Acceleration = 10f;
			Controller.ApplyFriction(5f);
			if ( Input.Pressed( "Jump" ) )
			{
				
				Controller.Punch(Vector3.Up * JumpStrength );
				if(Animator != null)
					Animator.TriggerJump();
			}

			if ( Input.Down( "Duck" ) )
			{
				if(Animator != null)
					Animator.DuckLevel = 5f;
				Controller.Height = 40f;
			}
			else
			{
				
				if(Animator != null)
					Animator.DuckLevel = 0f;
				Controller.Height = 64f;
			}
		}
		else
		{
			Controller.Acceleration = 1f;	
			Controller.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;
		}

		Controller.Move();
		
		if ( Animator != null )
		{
			Animator.IsGrounded = Controller.IsOnGround;
			Animator.WithVelocity(Controller.Velocity);

			if ( _lastPunch >= 2f )
			{
				Animator.HoldType = CitizenAnimationHelper.HoldTypes.None;
			}
		}

		if ( Input.Pressed( "attack1" ) && _lastPunch >= PunchCooldown )
		{
			Punch();
		}
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

	protected override void OnStart()
	{
		if( Camera != null )
			_initialCameraTransform = Camera.Transform.Local;
		if ( ModelRenderer.Components.TryGet<SkinnedModelRenderer>( out var renderer ) )
		{
			var clothing = ClothingContainer.CreateFromLocalUser();
			clothing.Apply( renderer );
		}
		Log.Info( EyePosition.ToString() );
			
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();
	}
	
	private void Punch()
	{
		if ( Animator != null )
		{
			Animator.HoldType = CitizenAnimationHelper.HoldTypes.Punch;
			Animator.Target.Set("b_attack",true);
		}
		var morphTrace = Scene.Trace.FromTo( EyeWorldPosition, EyeWorldPosition + EyeAngles.Forward * PunchRange )
			.Size( 20f )
			.WithoutTags( "player" )
			.IgnoreGameObject( GameObject )
			.Run();

		if ( morphTrace.Hit )
		{
			if ( morphTrace.Component.Components.TryGet<PropInfo>( out var unit ) )
			{
				
				if ( ModelRenderer.Components.TryGet<SkinnedModelRenderer>( out var renderer ) )
				{
					var clothing = ClothingContainer.CreateFromLocalUser();
					clothing.Reset( renderer );
				}

				if ( ModelRenderer.Components.TryGet<SkinnedModelRenderer>( out var playerRenderer ) )
				{
					playerRenderer.Model = Model.Load( unit.Model.Model.Name );
					playerRenderer.Transform.LocalPosition =
						new Vector3( 0f, 0f, unit.Model.Transform.LocalPosition.z );
				}
				Controller.Radius = unit.Collider.Scale.x / 2f;
				EyePosition = new Vector3(0f,0f,unit.Collider.Scale.z / 2f);
				Log.Info( EyePosition.ToString() );



			}
			
		}
		_lastPunch = 0f;
	}
	
	

	[Button("test", "")]
	void HoverEffect()
	{
		
		Log.Info( EyePosition.ToString() );
		Log.Info( EyeWorldPosition.ToString() );
	}

}
