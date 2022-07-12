/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Oculus.Interaction.Input
{
    public class HandPhysicsCapsules : MonoBehaviour
    {
        [SerializeField] private HandVisual _handVisual;

        private GameObject _capsulesGO;
        private List<BoneCapsule> _capsules;
        public IList<BoneCapsule> Capsules { get; private set; }
        private OVRPlugin.Skeleton2 _skeleton;
        private bool _capsulesAreActive;
        protected bool _started;
        public PhysicMaterial physicalMaterial;
        protected virtual void Awake()
        {
            Assert.IsNotNull(_handVisual);
            /*
             * following added by Wei
             */
            physicalMaterial = Resources.Load<PhysicMaterial>("New Physic Material");
    }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            _skeleton = _handVisual.Hand.Handedness == Handedness.Left
                ? OVRSkeletonData.LeftSkeleton
                : OVRSkeletonData.RightSkeleton;
            _capsulesGO = new GameObject("Capsules");
            _capsulesGO.transform.SetParent(transform, false);
            _capsulesGO.transform.localPosition = Vector3.zero;
            _capsulesGO.transform.localRotation = Quaternion.identity;

            _capsules = new List<BoneCapsule>(new BoneCapsule[_skeleton.NumBoneCapsules]);
            Capsules = _capsules.AsReadOnly();

            for (int i = 0; i < _capsules.Count; ++i)
            {
                Transform boneTransform = _handVisual.Joints[_skeleton.BoneCapsules[i].BoneIndex];
                BoneCapsule capsule = new BoneCapsule();
                _capsules[i] = capsule;

                capsule.BoneIndex = _skeleton.BoneCapsules[i].BoneIndex;

                /*added by Wei*/
                GameObject temp_CollisionPart = new GameObject((boneTransform.name).ToString() + "_CapsulePhysics");
                capsule.CapsuleRigidbody = temp_CollisionPart.AddComponent<Rigidbody>();
                /*
                 * the original version of two lines above is:
                 * capsule.CapsuleRigidbody = new GameObject((boneTransform.name).ToString() + "_CapsuleRigidbody")
                    .AddComponent<Rigidbody>();
                 */
                capsule.CapsuleRigidbody.mass = 1.0f;
                capsule.CapsuleRigidbody.isKinematic = true;
                capsule.CapsuleRigidbody.useGravity = false;
                capsule.CapsuleRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

                GameObject rbGO = capsule.CapsuleRigidbody.gameObject;
                rbGO.transform.SetParent(_capsulesGO.transform, false);
                rbGO.transform.position = boneTransform.position;
                rbGO.transform.rotation = boneTransform.rotation;
                rbGO.SetActive(false);

                //added by Wei
                capsule.CapsuleCollider = temp_CollisionPart.AddComponent<CapsuleCollider>();
                capsule.CapsuleCollider.material = physicalMaterial;
                /*
                 * the original version of the line above is:
                 * capsule.CapsuleCollider = new GameObject((boneTransform.name).ToString() + "_CapsuleCollider")
                    .AddComponent<Collider>();
                 */
                capsule.CapsuleCollider.isTrigger = false;

                var p0 = _skeleton.BoneCapsules[i].StartPoint.FromFlippedXVector3f();
                var p1 = _skeleton.BoneCapsules[i].EndPoint.FromFlippedXVector3f();
                var delta = p1 - p0;
                var mag = delta.magnitude;
                var rot = Quaternion.FromToRotation(Vector3.right, delta);
                capsule.CapsuleCollider.radius = _skeleton.BoneCapsules[i].Radius;
                capsule.CapsuleCollider.height = mag + _skeleton.BoneCapsules[i].Radius * 2.0f;
                capsule.CapsuleCollider.direction = 0;
                capsule.CapsuleCollider.center = Vector3.right * mag * 0.5f;

                GameObject ccGO = capsule.CapsuleCollider.gameObject;
                ccGO.transform.SetParent(rbGO.transform, false);
                ccGO.transform.localPosition = p0;
                ccGO.transform.localRotation = rot;
            }
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _handVisual.WhenHandVisualUpdated += HandleHandVisualUpdated;
            }
        }
        protected virtual void OnDisable()
        {
            if (_started)
            {
                _handVisual.WhenHandVisualUpdated -= HandleHandVisualUpdated;

                if (_capsules != null)
                {
                    for (int i = 0; i < _capsules.Count; ++i)
                    {
                        var capsuleGO = _capsules[i].CapsuleRigidbody.gameObject;
                        capsuleGO.SetActive(false);
                    }
                    _capsulesAreActive = false;
                }
            }
        }

        protected virtual void FixedUpdate()
        {
            if (_capsulesAreActive && !_handVisual.IsVisible)
            {
                for (int i = 0; i < _capsules.Count; ++i)
                {
                    var capsuleGO = _capsules[i].CapsuleRigidbody.gameObject;
                    capsuleGO.SetActive(false);
                }
                _capsulesAreActive = false;
            }
        }

        private void HandleHandVisualUpdated()
        {
            _capsulesAreActive = _handVisual.IsVisible;

            for (int i = 0; i < _capsules.Count; ++i)
            {
                BoneCapsule capsule = _capsules[i];
                var capsuleGO = capsule.CapsuleRigidbody.gameObject;

                if (_capsulesAreActive)
                {
                    Transform boneTransform = _handVisual.Joints[(int)capsule.BoneIndex];

                    if (capsuleGO.activeSelf)
                    {
                        capsule.CapsuleRigidbody.MovePosition(boneTransform.position);
                        capsule.CapsuleRigidbody.MoveRotation(boneTransform.rotation);
                    }
                    else
                    {
                        capsuleGO.SetActive(true);
                        capsule.CapsuleRigidbody.position = boneTransform.position;
                        capsule.CapsuleRigidbody.rotation = boneTransform.rotation;
                    }
                }
                else
                {
                    if (capsuleGO.activeSelf)
                    {
                        capsuleGO.SetActive(false);
                    }
                }
            }
        }

        #region Inject

        public void InjectAllOVRHandPhysicsCapsules(HandVisual hand)
        {
            InjectHandSkeleton(hand);
}

        public void InjectHandSkeleton(HandVisual hand)
        {
            _handVisual = hand;
        }

        #endregion
    }

    public class BoneCapsule
    {
        public short BoneIndex { get; set; }
        public Rigidbody CapsuleRigidbody { get; set; }
        public CapsuleCollider CapsuleCollider { get; set; }
    }
}