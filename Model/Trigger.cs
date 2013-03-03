using System;

namespace HomeView.Model
{
    /// <summary>
    /// The base abstract Trigger class dictates all of the required functionality that a trigger class should
    /// have implemented, including any future triggers that could be developed and included in the
    /// application.
    /// </summary>
    abstract class Trigger
    {
        //Active boolean status
        private bool m_Active;
        //Triggered event to be fired when detection is made
        event EventHandler triggered;

        protected bool Active
        {
            get { return m_Active; }
            set { m_Active = value; }
        }

        public Trigger()
        {
            m_Active = false;
        }
    }
}
