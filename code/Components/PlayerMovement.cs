using Sandbox;
using Sandbox.Citizen;

public sealed class PlayerMovement : Component
{

	[Property] public float GroundControl { get; set; } = 4.0f;
	[Property] public float AirControl { get; set; } = 0.1f;
	[Property] public float MaxForce { get; set; } = 50f;

	[Property] public float Speed { get; set; } = 160f;

	[Property] public float RunSpeed { get; set; } = 290f;

	[Property] public float CrouchSpeed { get; set; } = 90f;
	[Property] public float JumpForce { get; set; } = 400f;

	// Object References

	[Property] public GameObject Head { get; set; }
	[Property] public GameObject Body { get; set; }

	// Member Variables

	public Vector3 WishVelocity = Vector3.Zero;

	public bool IsCrouching = false;

	public bool IsSprinting = false;
	private CharacterController characterController;
	private CitizenAnimationHelper animationHelper;

	protected override void OnAwake()
	{
		characterController = Components.Get<CharacterController>();
		animationHelper = Components.Get<CitizenAnimationHelper>();
	}
	protected override void OnUpdate()
	{
		IsCrouching = Input.Down( "Crouch" );
		IsSprinting = Input.Down( "Run" );
		if(Input.Pressed( "Jump" )) Jump();

		RotateBody();

	}

	protected override void OnFixedUpdate()
	{
		BuildWishVelocity();
		RotateBody();
		Move();
	}

	void BuildWishVelocity()
	{
		WishVelocity = 0;

		var rot = Head.Transform.Rotation;
		if (Input.Down( "Forward" )) WishVelocity += rot.Forward;
		if (Input.Down( "Backward" )) WishVelocity += rot.Backward;
		if (Input.Down( "Left" )) WishVelocity += rot.Left;
		if (Input.Down( "Right" )) WishVelocity += rot.Right;

		WishVelocity = WishVelocity.WithZ(0);
		if (!WishVelocity.IsNearZeroLength) WishVelocity = WishVelocity.Normal;

		if (IsCrouching) WishVelocity *= CrouchSpeed;
		else if (IsSprinting) WishVelocity *= RunSpeed;
		else WishVelocity *= Speed;
	}

	void Move()
	{
		// Get gravity for scene
		var gravity = Scene.PhysicsWorld.Gravity;

		if(characterController.IsOnGround)
		{
			characterController.Velocity = characterController.Velocity.WithZ(0);
			characterController.Accelerate(WishVelocity);
			characterController.ApplyFriction(GroundControl);
		}
		else
		{
			characterController.Velocity += gravity * Time.Delta * 0.5f;
			characterController.Accelerate(WishVelocity.ClampLength(MaxForce));
			characterController.ApplyFriction(AirControl);
		}

		// Move Character Controller

		characterController.Move();

		// Apply second half of gravity after movement

		if(!characterController.IsOnGround)
		{
			characterController.Velocity += gravity * Time.Delta * 0.5f;
		}
		else
		{
			characterController.Velocity = characterController.Velocity.WithZ(0);
		}
	}

	void RotateBody()
	{
		if(Body is null) return;

		var targetAngle = new Angles(0, Head.Transform.Rotation.Yaw(), 0).ToRotation();
		float rotateDifference = Body.Transform.Rotation.Distance(targetAngle);

		if(rotateDifference > 50f || characterController.Velocity.Length > 10f)
		{
			Body.Transform.Rotation = Rotation.Lerp(Body.Transform.Rotation, targetAngle, Time.Delta * 2f);
		}
	}
	void Jump()
	{
		if(!characterController.IsOnGround) return;

		characterController.Punch(Vector3.Up * JumpForce);
		animationHelper?.TriggerJump();

	}
}