//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Collections;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    public abstract class Entity : EntityLogic
    {

        [SerializeField]
        private EntityData m_EntityData = null;


        public int Id
        {
            get { return Entity.Id; }
        }
        public int TypeId
        {
            get { return m_EntityData == null ? 0 : m_EntityData.TypeId; }
        }

        public virtual void DelayHide(float delay = 0f)
        {
            StartCoroutine(_delayHide(delay));
        }

        public virtual void DelaySetActiveFalse(float delay = 0f)
        {
            StartCoroutine(_delaySetActiveFalse(delay));
        }

        protected IEnumerator _delayHide(float delay)
        {
            yield return new WaitForSeconds(delay);
            GameEntry.Entity.HideEntity(this);
        }
        protected IEnumerator _delaySetActiveFalse(float delay)
        {
            yield return new WaitForSeconds(delay);

            this.gameObject.SetActive(false);
        }

        protected internal virtual void ReleaseData()
        {
            ReferencePool.Release(m_EntityData);
        }

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
        }


        protected override void OnRecycle()
        {
            base.OnRecycle();
        }


        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            m_EntityData = userData as EntityData;
            if (m_EntityData == null)
            {
                Log.Error("Entity data is invalid.");
                return;
            }

            //Name = Utility.Text.Format("[Entity {0}]", Id);
            // 生成数据传入的是世界坐标；对象池复用时须用 world 而非 local，避免父节点非原点时偏移。
            CachedTransform.SetPositionAndRotation(m_EntityData.Position, m_EntityData.Rotation);
            //CachedTransform.localScale = Vector3.one;
        }


        protected override void OnHide(bool isShutdown, object userData)
        {
            base.OnHide(isShutdown, userData);
            StopAllCoroutines();
            ReleaseData();
        }


        protected override void OnAttached(EntityLogic childEntity, Transform parentTransform, object userData)
        {
            base.OnAttached(childEntity, parentTransform, userData);
        }


        protected override void OnDetached(EntityLogic childEntity, object userData)
        {
            base.OnDetached(childEntity, userData);
        }


        protected override void OnAttachTo(EntityLogic parentEntity, Transform parentTransform, object userData)
        {
            base.OnAttachTo(parentEntity, parentTransform, userData);
        }


        protected override void OnDetachFrom(EntityLogic parentEntity, object userData)
        {
            base.OnDetachFrom(parentEntity, userData);
        }


        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
        }

        
    }
}
