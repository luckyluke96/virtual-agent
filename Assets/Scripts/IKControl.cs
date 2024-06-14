using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(Animator))] 

public class IKControl : MonoBehaviour {
    
    protected Animator animator;
    
    [Header("Head")]
    [Range(0, 1)]
    public float ikWeightHead = 0.0f;
    public Transform targetHead = null;
    public RealisticEyeMovements.LookTargetController lookTargetController = null;

    [Header("Left Hand")]
    [Range(0, 1)]
    public float ikWeightLeftHand = 0.0f;
    public Transform targetLeftHand = null;

    [Header("Right Hand")]
    [Range(0, 1)]
    public float ikWeightRightHand = 0.0f;
    public Transform targetRightHand = null;

    void Start () 
    {
        animator = GetComponent<Animator>();
    }
    
    void OnAnimatorIK()
    {
        if(animator) 
        {
            // Head control
            if(targetHead != null) 
            {
                if(lookTargetController != null)
                {
                    if(ikWeightHead > 0.0f)
                    {
                        lookTargetController.LookAtPoiDirectly(targetHead.position);
                    }
                }
                else
                {
                    animator.SetLookAtWeight(ikWeightHead);
                    animator.SetLookAtPosition(targetHead.position);
                }
            }


            // Left hand
            if (targetLeftHand != null) 
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, ikWeightLeftHand);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, ikWeightLeftHand);  
                animator.SetIKPosition(AvatarIKGoal.LeftHand, targetLeftHand.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, targetLeftHand.rotation);
            }  

            // Right hand
            if(targetRightHand != null) 
            {
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, ikWeightRightHand);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, ikWeightRightHand);  
                animator.SetIKPosition(AvatarIKGoal.RightHand, targetRightHand.position);
                animator.SetIKRotation(AvatarIKGoal.RightHand, targetRightHand.rotation);
            }  
        }
    } 
}