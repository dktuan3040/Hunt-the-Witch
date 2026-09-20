using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponShoot : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Projectile projectilePrefab;

    [Header("Shooting Settings")]
    [SerializeField] private float fireRate = 0.25f;

    [Header("Input")]
    [SerializeField] private InputActionReference shootAction;

    private float nextFireTime;

    private void OnEnable()
    {
        if(shootAction != null)
        {
            shootAction.action.performed += OnShoot;
            shootAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if(shootAction != null)
        {
            shootAction.action.performed -= OnShoot;
            shootAction.action.Disable();
        }
    }

    private void OnShoot(InputAction.CallbackContext context)
    {
        Shoot();
    }

    private void Shoot()
    {
        if(Time.time < nextFireTime)
        {
            return;
        }
        if(firePoint == null || projectilePrefab == null)
        {
            Debug.LogWarning("Fire point hoac Projectile chua gan");
            return;
        }

        nextFireTime = Time.time + fireRate;

        // tao projectile moi vi tri dau gay
        Projectile projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        // chinh huong ban
        Vector2 shootDirection = firePoint.right;
        projectile.Initialize(shootDirection);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
