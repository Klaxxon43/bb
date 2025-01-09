using System;
using System.Collections;
using UnityEngine;

[SelectionBase]
public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }
    public event EventHandler OnPlayerDeath;

    [SerializeField] private float _movingSpeed = 10f;
    [SerializeField] private int _maxHealth = 10;
    [SerializeField] private float _damageRecoveryTime = 0.5f;

    private Vector2 inputVector;
    private Rigidbody2D _rb;
    private KnockBack _knockBack;

    private float _minMovingSpeed = 0.1f;
    private bool _isRunning = false;
    private int _currentHealth;
    private bool _canTakeDamage;
    private bool _isAlive;

    private Transform target; // Это поле будет заполнено в инспекторе Unity
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 5, -10); // Инициализация offset здесь


    private void Awake()
    {
        Instance = this;
        _rb = GetComponent<Rigidbody2D>();
        _knockBack = GetComponent<KnockBack>();
    }

    private void Start()
    {
        _currentHealth = _maxHealth;
        _canTakeDamage = true;
        if (GameInput.Instance != null) GameInput.Instance.OnPlayerAttack += GameInput_OnPlayerAttack;
        _isAlive = true;
        //Проверка на наличие объекта target
        target = GameObject.Find("Player").transform;
        if (target == null)
        {
            Debug.LogError("Target not assigned in the inspector!");
        }
        else
        {
            LateUpdate();
        }

    }

    private void GameInput_OnPlayerAttack(object sender, System.EventArgs e)
    {
        if (ActiveWeapon.Instance != null) ActiveWeapon.Instance.GetActiveWeapon().Attack();
    }

    private void Update()
    {
        inputVector = GameInput.Instance.GetMovementVector();
        LateUpdate();
    }

    private void FixedUpdate()
    {
        if (_knockBack != null && _knockBack.IsGettingKnockedBack) return;
        HandleMovement();
    }

    public bool IsAlive() => _isAlive;

    public void TakeDamage(Transform damageSource, int damage)
    {
        if (_canTakeDamage && _isAlive)
        {
            _canTakeDamage = false;
            _currentHealth = Mathf.Max(0, _currentHealth - damage); // Исправлено вычитание
            Debug.Log(_currentHealth);
            if (_knockBack != null) _knockBack.GetKnockedBack(damageSource);
            StartCoroutine(DamageRecoveryRoutine());
        }
        DetectDeath();
    }

    private void DetectDeath()
    {
        if (_currentHealth == 0 && _knockBack != null && _isAlive)
        {
            _isAlive = false;
            _knockBack.StopKnockBackMovement(); // Исправлена опечатка
            GameInput.Instance.DisableMovement();
            OnPlayerDeath?.Invoke(this, EventArgs.Empty);
        }
    }

    private IEnumerator DamageRecoveryRoutine()
    {
        yield return new WaitForSeconds(_damageRecoveryTime);
        _canTakeDamage = true;
    }

    private void HandleMovement()
    {
        if (_rb != null) _rb.MovePosition(_rb.position + inputVector * (_movingSpeed * Time.fixedDeltaTime));
        _isRunning = Mathf.Abs(inputVector.x) > _minMovingSpeed || Mathf.Abs(inputVector.y) > _minMovingSpeed;
    }

    public bool IsRunning()
    {
        return _isRunning;
    }

    public Vector3 GetPlayerScreenPosition()
    {
        return Camera.main.WorldToScreenPoint(transform.position);
    }



    void LateUpdate()
    {
        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}

