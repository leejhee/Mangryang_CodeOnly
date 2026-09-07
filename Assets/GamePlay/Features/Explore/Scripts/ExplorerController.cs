using Core.Scripts.Managers;
using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Contracts.Interaction;
using GamePlay.Common.Scripts.Interaction;
using System.Threading;
using Unity.Cinemachine;
using UnityEngine;


namespace GamePlay.Features.Explore.Scripts
{
    /// <summary>
    /// '탐사' 씬에서 유저가 조종할 컨트롤러.
    /// </summary>
    public class ExploreController : Interactor
    {
        private static readonly int Move = Animator.StringToHash("Move");
        [SerializeField] private float speed = 3f;

        //public Transform Transform { get; }
        //public CancellationToken LifeTimeToken { get; }
        public Transform CameraTransform;

        private Rigidbody2D _rb;
        private Vector2 _moveInput;
        
        private Animator animator;
        private SpriteRenderer spriteRenderer;
        
        
        private IInteractable _focused;
        private bool _isInteracting;
        
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            CinemachineCamera cam = CameraTransform.gameObject.GetComponent<CinemachineCamera>();
            cam.Lens.OrthographicSize = 1f;
        }
        
        private void Update()
        {
            if (InputManager.Instance.GetExploreInteractDown())
            {
                TryInteractCurrent().Forget();
            }
        }

        private void FixedUpdate()
        {
            Vector2 move = InputManager.Instance.GetExploreMove();


            if (move != Vector2.zero)
            {
                animator.SetBool(Move, true);

                if (move.x > 0)
                {
                    spriteRenderer.flipX = false;
                }
                else if (move.x < 0)
                {
                    spriteRenderer.flipX = true;
                }
            }
            else if (move == Vector2.zero)
            {
                animator.SetBool(Move, false);
            }
            
            
            if (move.sqrMagnitude > 1f)
                move.Normalize();
            
            Vector2 delta = move * (speed * Time.fixedDeltaTime);
            _rb.MovePosition(_rb.position + delta);
        }

        
        private async UniTaskVoid TryInteractCurrent()
        {
            if (_isInteracting) return;          // 이미 상호작용 중이면 무시
            if (_focused == null) return;        // 대상 없으면 무시

            // 상호작용 가능 상태인지 한 번 더 체크 
            if (!_focused.Interactable(this))
                return;

            _isInteracting = true;
            try
            {
                await TryInteract(_focused, CancellationToken.None);
            }
            finally
            {
                _isInteracting = false;
            }
        }
        
        // 상호작용 전용 이벤트
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out IInteractable candidate))
            {
                if (_focused == null || candidate.Priority >= _focused.Priority)
                {
                    _focused?.OnFocusExit(this);
                    _focused = candidate;
                    _focused.OnFocusEnter(this);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (_focused == null) return;
            
            if (other.TryGetComponent(out IInteractable candidate) &&
                _focused == candidate)
            {
                candidate.OnFocusExit(this);
                _focused = null;
            }
        }
        
        
    }
}

