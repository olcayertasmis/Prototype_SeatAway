using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using PSA.Core;
using PSA.Gameplay.Data;
using PSA.Gameplay.Grid;
using PSA.Gameplay.Seats;

namespace PSA.Gameplay.Passengers
{
    public class Passenger : MonoBehaviour
    {
        [Header("Passenger Visuals")]
        [SerializeField] private MeshRenderer meshRenderer;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed;
        [SerializeField] private float walkOffsetY;

        [Header("Sit Settings")]
        [SerializeField] private float jumpPowerOnSit;
        [SerializeField] private float sitDuration;

        [Header("Data")]
        private static MaterialPropertyBlock _mpb;
        private Seat _targetSeat;
        private Cell _currentCell;
        private ElementColor _color;
        private Cell _targetSlot;

        [Header("Controls")]
        private bool _isMoving;

        #region Encapsulation

        public Cell CurrentCell => _currentCell;
        public ElementColor Color => _color;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
        }

        #endregion

        #region ISystem Implementation

        public void Initialize(Cell startCell, ElementColor color)
        {
            _currentCell = startCell;
            _color = color;

            Vector3 startPos = startCell.transform.position;
            startPos.y += walkOffsetY;
            transform.position = startPos;

            Color matColor = SetPassengerColor(_color);

            meshRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor("_Color", matColor);
            _mpb.SetColor("_BaseColor", matColor);
            meshRenderer.SetPropertyBlock(_mpb);
        }

        #endregion

        #region Core Logic

        private Color SetPassengerColor(ElementColor elementColor)
        {
            return elementColor switch
            {
                ElementColor.Blue => UnityEngine.Color.blue,
                ElementColor.Green => UnityEngine.Color.green,
                ElementColor.Red => UnityEngine.Color.red,
                ElementColor.Yellow => UnityEngine.Color.yellow,
                _ => UnityEngine.Color.white
            };
        }

        public void MoveAlongPath(List<Cell> path, Seat targetSeat, Cell targetSlot)
        {
            if (_isMoving) return;

            _isMoving = true;
            _targetSeat = targetSeat;
            _targetSlot = targetSlot;

            StartCoroutine(FollowPathRoutine(path));
        }

        private IEnumerator FollowPathRoutine(List<Cell> path)
        {
            foreach (Cell nextCell in path)
            {
                float duration = 1f / moveSpeed;

                Vector3 targetPos = nextCell.transform.position;
                targetPos.y += walkOffsetY;

                transform.DOLookAt(targetPos, 0.1f);
                transform.DOMove(targetPos, duration).SetEase(Ease.Linear);

                yield return new WaitForSeconds(duration);

                _currentCell = nextCell;
            }

            SitOnSeat();
        }

        private void SitOnSeat()
        {
            _isMoving = false;

            _targetSeat.SeatPassengerAtSlot(_targetSlot);
            transform.SetParent(_targetSeat.transform);

            Vector3 targetLocalPos = _targetSeat.transform.InverseTransformPoint(_targetSlot.transform.position);
            targetLocalPos.y = 0.5f;

            transform.DOLocalJump(targetLocalPos, jumpPowerOnSit, 1, sitDuration).SetEase(Ease.OutQuad);

            transform.DOLocalRotate(Vector3.zero, sitDuration).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                transform.localPosition = targetLocalPos;
                transform.localRotation = Quaternion.identity;

                SystemLocator.Get<EventManager>().TriggerEvent(new PassengerSeatedEvent { seat = _targetSeat });
            });
        }

        public void StopMovement()
        {
            StopAllCoroutines();

            transform.DOKill();

            _isMoving = false;
        }

        #endregion
    }
}