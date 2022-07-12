using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction
{
    public class OneHandPickTransformer : MonoBehaviour, ITransformer
    {
        public bool isDropped = false;

        public bool isPickedUp = false;

        [SerializeField]
        private SnapPoint[] _snapPoints = { };

        [SerializeField]
        private float _snapPointPositionEaseTime;
        [SerializeField]
        private float _snapPointRotationEaseTime;

        public float SnapPointPositionEaseTime
        {
            get
            {
                return _snapPointPositionEaseTime;
            }
            set
            {
                _snapPointPositionEaseTime = value;
            }
        }

        public float SnapPointRotationEaseTime
        {
            get
            {
                return _snapPointRotationEaseTime;
            }
            set
            {
                _snapPointRotationEaseTime = value;
            }
        }

        private Vector3 _initialPositionInGrabSpace;
        private Vector3 _desiredPositionInGrabSpace;

        private Quaternion _initialRotationInGrabSpace;
        private Quaternion _desiredRotationInGrabSpace;
        private float _transformStartTime;

        private ITransformable _transformable;

        public void Initialize(ITransformable transformable)
        {
            _transformable = transformable;
        }

        public void BeginTransform()
        {
            isPickedUp = true;
            Vector3 grabPointPosition = _transformable.GrabPoints[0].GrabPosition;
            Quaternion grabPointRotation = _transformable.GrabPoints[0].GrabRotation;
            // find the closest snap point
            SnapPoint closestSnapPoint = null;
            float closestSnapPointDistSq = float.MaxValue;
            for (int i = 0; i < _snapPoints.Length; i++)
            {
                SnapPoint snapPoint = _snapPoints[i];
                float distSq = (grabPointPosition - snapPoint.transform.position).sqrMagnitude;
                if (distSq < closestSnapPointDistSq)
                {
                    closestSnapPoint = snapPoint;
                    closestSnapPointDistSq = distSq;
                }
            }

            // calculate initial and desired positions and rotations in grab-point space
            // first: calculate the desired transform of the snap point
            Transform snapPointTransform = closestSnapPoint != null ? closestSnapPoint.transform
                                            : _transformable.Transform;

            Vector3 snapPointPosition = snapPointTransform.position;
            bool lerpToSnapPosition = false;

            if (closestSnapPoint != null)
            {
                Collider snapPointCollider = closestSnapPoint.Collider;

                if (snapPointCollider == null)
                {
                    lerpToSnapPosition = true;
                }
                else
                {
                    Vector3 targetPosition = snapPointPosition;
                    snapPointPosition = _transformable.Transform.position;
                    if (!Collisions.IsPointWithinCollider(grabPointPosition, snapPointCollider))
                    {
                        RaycastHit hitInfo;
                        Vector3 toSnapPoint = targetPosition - grabPointPosition;
                        if (snapPointCollider.Raycast(new Ray(grabPointPosition, toSnapPoint), out hitInfo, Mathf.Infinity))
                        {
                            Vector3 collisionPoint = hitInfo.point;
                            snapPointPosition = collisionPoint - closestSnapPoint.DistanceThreshold * toSnapPoint.normalized;
                            lerpToSnapPosition = true;
                        }
                    }
                }
            }

            Vector3 grabPointToSnapPoint = snapPointPosition - grabPointPosition;
            Vector3 snapPointPositionInGrabSpace =
                Quaternion.Inverse(grabPointRotation) * grabPointToSnapPoint;
            Quaternion snapPointRotationInGrabSpace =
                Quaternion.Inverse(grabPointRotation) *
                snapPointTransform.rotation;

            Vector3 desiredSnapPointPositionInGrabSpace = lerpToSnapPosition ? Vector3.zero : snapPointPositionInGrabSpace;

            Quaternion desiredSnapPointRotationInGrabSpace = Quaternion.identity;

            // second: calculate the desired transform of the _transformable object itself
            Vector3 snapPointToObject =
                _transformable.Transform.position - snapPointPosition;
            Vector3 objectPositionInSnapPointSpace = Quaternion.Inverse(snapPointTransform.rotation)
                                                     * snapPointToObject;
            Quaternion objectRotationInSnapPointSpace = Quaternion.Inverse(snapPointTransform.rotation)
                                                        * _transformable.Transform.rotation;
            _initialPositionInGrabSpace =
                snapPointPositionInGrabSpace + (snapPointRotationInGrabSpace * objectPositionInSnapPointSpace);

            Quaternion rotationToUseForDesiredPosition = _snapPointRotationEaseTime > 0
                ? desiredSnapPointRotationInGrabSpace
                : snapPointRotationInGrabSpace;
            _desiredPositionInGrabSpace =
                desiredSnapPointPositionInGrabSpace + (rotationToUseForDesiredPosition * objectPositionInSnapPointSpace);

            _initialRotationInGrabSpace = snapPointRotationInGrabSpace * objectRotationInSnapPointSpace;
            _desiredRotationInGrabSpace = desiredSnapPointRotationInGrabSpace * objectRotationInSnapPointSpace;

            _transformStartTime = Time.realtimeSinceStartup;
        }

        public void UpdateTransform()
        {
            var grabPoint = _transformable.GrabPoints[0];
            var targetTransform = _transformable.Transform;

            float time = Time.realtimeSinceStartup - _transformStartTime;

            Vector3 positionInGrabSpace = _initialPositionInGrabSpace;
            if (_snapPointPositionEaseTime > 0)
            {
                float t = Mathf.Clamp01(time / _snapPointPositionEaseTime);
                positionInGrabSpace = new Vector3(
                    Mathf.SmoothStep(_initialPositionInGrabSpace.x, _desiredPositionInGrabSpace.x, t),
                    Mathf.SmoothStep(_initialPositionInGrabSpace.y, _desiredPositionInGrabSpace.y, t),
                    Mathf.SmoothStep(_initialPositionInGrabSpace.z, _desiredPositionInGrabSpace.z, t)
                );
            }

            Quaternion rotationInGrabSpace = _initialRotationInGrabSpace;
            if (_snapPointRotationEaseTime > 0)
            {
                float t = Mathf.Clamp01(time / _snapPointRotationEaseTime);
                rotationInGrabSpace =
                    Quaternion.Slerp(_initialRotationInGrabSpace, _desiredRotationInGrabSpace, t);
            }

            targetTransform.position = (grabPoint.GrabRotation * positionInGrabSpace) + grabPoint.GrabPosition;
            targetTransform.rotation = grabPoint.GrabRotation * rotationInGrabSpace;
        }

        public void EndTransform() {
            Debug.Log("#####The transform is over");
            isDropped = true;
        }
    }
}
