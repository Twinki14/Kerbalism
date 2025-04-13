using Kerbalism.System;

namespace Kerbalism.Planner
{
    ///<summary> Planners simulator for the environment the vessel is presently in, according to the planners environment settings </summary>
    public sealed class EnvironmentAnalyzer
    {
        public void Analyze(CelestialBody body, double altitude_mult, Planner.SunlightState sunlight)
        {
            this.body = body;
            CelestialBody mainSun;
            Vector3d sun_dir;
            solar_flux = Sim.SolarFluxAtBody(body, true, out mainSun, out sun_dir, out sun_dist);
            altitude = body.Radius * altitude_mult;
            landed = altitude <= double.Epsilon;
            atmo_factor = Sim.AtmosphereFactor(body, 0.7071);
            solar_flux = sunlight == Planner.SunlightState.Shadow ? 0.0 : solar_flux * (landed ? atmo_factor : 1.0);
            breathable = Sim.Breathable(body) && landed;
            albedo_flux = sunlight == Planner.SunlightState.Shadow
                ? 0.0
                : Sim.AlbedoFlux(body, body.position + sun_dir * (body.Radius + altitude));
            body_flux = Sim.BodyFlux(body, altitude);
            total_flux = solar_flux + albedo_flux + body_flux + Sim.BackgroundFlux();
            temperature = !landed || !body.atmosphere ? Sim.BlackBodyTemperature(total_flux) : body.GetTemperature(0.0);
            orbital_period = Sim.OrbitalPeriod(body, altitude);
            shadow_period = Sim.ShadowPeriod(body, altitude);
            shadow_time = shadow_period / orbital_period;
            zerog = !landed && (!body.atmosphere || body.atmosphereDepth < altitude);

            var b = body;
            while (b != null && b.orbit != null && b != mainSun)
            {
                if (b == b.referenceBody) break;
                b = b.referenceBody;
            }
        }

        public CelestialBody body; // target body
        public double altitude; // target altitude
        public bool landed; // true if landed
        public bool breathable; // true if inside breathable atmosphere
        public bool zerog; // true if the vessel is experiencing zero g
        public double atmo_factor; // proportion of sun flux not absorbed by the atmosphere
        public double sun_dist; // distance from the sun
        public double solar_flux; // flux received from the sun (consider atmospheric absorption)
        public double albedo_flux; // solar flux reflected from the body
        public double body_flux; // infrared radiative flux from the body
        public double total_flux; // total flux at vessel position
        public double temperature; // vessel temperature
        public double orbital_period; // length of orbit
        public double shadow_period; // length of orbit in shadow
        public double shadow_time; // proportion of orbit that is in shadow
    }
} // KERBALISM
