namespace RadarSoft.RadarCube.ClientAgents
{
    public class Wrapper<T>
    {
        public T Value;

        internal Wrapper(T c)
        {
            Value = c;
        }
    }

    public class Wrapper<T1, T2>
    {
        public T1 Value1;
        public T2 Value2;

        internal Wrapper(T1 t1, T2 t2)
        {
            Value1 = t1;
            Value2 = t2;
        }
    }
}