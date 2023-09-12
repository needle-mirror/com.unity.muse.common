using UnityEngine.UIElements;

namespace Unity.Muse.Common
{
    internal class CooldownManipulator<TEventType> : Manipulator where TEventType : EventBase<TEventType>, new()
    {
        public bool IsInCooldown => m_IsOnCooldown;
        
        /// <summary>
        /// Cooldown time in seconds.
        /// </summary>
        private readonly float m_CooldownTime;
        private readonly bool m_DisableDuringCooldown;
        
        bool m_IsOnCooldown;
        
        public CooldownManipulator(bool disableDuringCooldown = true, float cooldownTime = 1.5f)
        {
            m_DisableDuringCooldown = disableDuringCooldown;
            m_CooldownTime = cooldownTime;
        }
        
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<TEventType>(OnEvent);
        }
        

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<TEventType>(OnEvent);
        }
        
        private void OnEvent(TEventType evt)
        {
            if (m_IsOnCooldown)
            {
                evt.StopPropagation();
                return;
            }
            
            target.schedule.Execute(() =>
            {
                if (m_DisableDuringCooldown)
                    target.SetEnabled(false);
                target.schedule.Execute(() =>
                {
                    if (m_DisableDuringCooldown)
                        target.SetEnabled(true);
                    m_IsOnCooldown = false;
                }).StartingIn((long)(m_CooldownTime * 1000f));
            }).StartingIn(0);
            
            m_IsOnCooldown = true;
        }

        public void ForceCooldown()
        {
            if(!m_IsOnCooldown)
                OnEvent(null);
        }
    }
}