using UnityEngine;

namespace Character 
{
    public class Characters : MonoBehaviour
    {
        protected Status Stat = new Status();
        [SerializeField] protected new Collider2D collider2D;
        [SerializeField] protected new Animator animator;
    }
}