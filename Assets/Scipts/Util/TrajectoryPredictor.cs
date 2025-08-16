// TrajectoryPredictor.cs
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryPredictor : MonoBehaviour
{
    [Header("Arc Settings")]
    public int arcSteps = 60;
    public bool useFixedDeltaTime = true;
    public LayerMask collisionMask;

    [Header("Match PlayerController Custom Gravity")]
    public bool useCustomGravity = true;
    public float gravityAcceleration = 25f;
    public float maxFallSpeed = 15f;
    public float ascentGravityMultiplier = 0.3f;
    public float fallingStartEpsilon = 0.1f;

    [Header("Linear Damping (Rigidbody2D.linearDamping)")]
    public bool useLinearDamping = true;
    public float linearDamping = 0f;

    [Header("Start Position")]
    public bool startAtColliderTop = true;
    public float arcStartYOffset = 0.03f;

    [Header("Collision Sampling")]
    [Tooltip("Shape cast can cause immediate self-hits when standing; leave OFF unless needed.")]
    public bool useShapeCast = false;
    public float minHitDistance = 0.0005f;

    [Header("Optional: player collider (offset/shape cast)")]
    public Collider2D playerCollider;

    private static readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[8];
    private LineRenderer _line;

    void Awake()
    {
        _line = GetComponent<LineRenderer>();
        _line.useWorldSpace = true;
        _line.enabled = false;
        _line.positionCount = 0;
    }

    public void DrawArc(Vector2 startPos, Vector2 initialVelocity)
    {
        if (!_line.enabled) _line.enabled = true;

        if (startAtColliderTop && playerCollider != null)
        {
            Bounds b = playerCollider.bounds;
            startPos = new Vector2(b.center.x, b.max.y + arcStartYOffset);
        }

        float dt = useFixedDeltaTime ? Time.fixedDeltaTime : Time.deltaTime;

        Vector2 pos = startPos;
        Vector2 vel = initialVelocity;
        float fallSpeed = 0f;

        var pts = new List<Vector3>(arcSteps) { new Vector3(pos.x, pos.y, 0f) };

        for (int i = 1; i < arcSteps; i++)
        {
            // gravity (mirror controller)
            if (useCustomGravity)
            {
                if (vel.y <= fallingStartEpsilon)
                {
                    fallSpeed += gravityAcceleration * dt;
                    if (fallSpeed > maxFallSpeed) fallSpeed = maxFallSpeed;
                    vel.y = -fallSpeed;
                }
                else
                {
                    vel.y -= gravityAcceleration * ascentGravityMultiplier * dt;
                    fallSpeed = 0f;
                }
            }
            else
            {
                vel += Physics2D.gravity * dt;
            }

            // linear damping (Unity 2D)
            if (useLinearDamping && linearDamping > 0f)
            {
                float factor = 1f / (1f + linearDamping * dt);
                vel *= factor;
            }

            Vector2 prev = pos;
            Vector2 next = pos + vel * dt;

            bool hitSomething = false;
            Vector2 hitPoint = Vector2.zero;

            if (useShapeCast && playerCollider != null)
            {
                var filter = new ContactFilter2D { useLayerMask = true, layerMask = collisionMask, useTriggers = false };
                Vector2 delta = next - prev;
                float distance = delta.magnitude;

                if (distance > 0f)
                {
                    int hits = playerCollider.Cast(delta / distance, filter, _hitBuffer, distance);
                    if (hits > 0 && _hitBuffer[0].distance > minHitDistance)
                    {
                        hitSomething = true;
                        hitPoint = _hitBuffer[0].point;
                    }
                }
            }
            else
            {
                RaycastHit2D hit = Physics2D.Linecast(prev, next, collisionMask);
                if (hit.collider != null && (hit.point - prev).sqrMagnitude > (minHitDistance * minHitDistance))
                {
                    hitSomething = true;
                    hitPoint = hit.point;
                }
            }

            if (hitSomething)
            {
                pts.Add(hitPoint);
                break;
            }

            pos = next;
            pts.Add(new Vector3(pos.x, pos.y, 0f));
        }

        _line.positionCount = pts.Count;
        _line.SetPositions(pts.ToArray());
    }

    public void HideArc()
    {
        _line.enabled = false;
        _line.positionCount = 0;
    }
}
