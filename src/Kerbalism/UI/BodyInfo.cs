using Kerbalism.System;

namespace Kerbalism
{
    public static class BodyInfo
    {
        public static void Body_info(this Panel p)
        {
            // only show in mapview
            if (!MapView.MapIsEnabled) return;

            // only show if there is a selected body and that body is not the sun
            var body = Lib.MapViewSelectedBody();
            if (body == null) return;

            // for all bodies except sun(s)
            if (!Lib.IsSun(body))
            {
                CelestialBody mainSun;
                double sun_dist;
                var solar_flux = Sim.SolarFluxAtBody(body, false, out mainSun, out var sun_dir, out sun_dist);
                solar_flux *= Sim.AtmosphereFactor(body, 0.7071);

                // calculate simulation values
                var albedo_flux = Sim.AlbedoFlux(body, body.position + sun_dir * body.Radius);
                var body_flux = Sim.BodyFlux(body, 0.0);
                var total_flux = solar_flux + albedo_flux + body_flux + Sim.BackgroundFlux();
                var temperature = body.atmosphere ? body.GetTemperature(0.0) : Sim.BlackBodyTemperature(total_flux);

                // calculate night-side temperature
                var total_flux_min = Sim.AlbedoFlux(body, body.position - sun_dir * body.Radius) + body_flux +
                                     Sim.BackgroundFlux();
                var temperature_min = Sim.BlackBodyTemperature(total_flux_min);

                // surface panel
                var temperature_str = body.atmosphere
                    ? Lib.HumanReadableTemp(temperature)
                    : Lib.BuildString(Lib.HumanReadableTemp(temperature_min), " / ",
                        Lib.HumanReadableTemp(temperature));
                p.AddSection(Local.BodyInfo_SURFACE); //"SURFACE"
                p.AddContent(Local.BodyInfo_temperature, temperature_str); //"temperature"
                p.AddContent(Local.BodyInfo_solarflux, Lib.HumanReadableFlux(solar_flux)); //"solar flux"

                // atmosphere panel
                if (body.atmosphere)
                {
                    p.AddSection(Local.BodyInfo_ATMOSPHERE); //"ATMOSPHERE"
                    p.AddContent(Local.BodyInfo_breathable,
                        Sim.Breathable(body)
                            ? Local.BodyInfo_breathable_yes
                            : Local.BodyInfo_breathable_no); //"breathable""yes""no"
                    p.AddContent(Local.BodyInfo_lightabsorption,
                        Lib.HumanReadablePerc(1.0 - Sim.AtmosphereFactor(body, 0.7071))); //"light absorption"
                }
            }

            // explain the user how to toggle the BodyInfo window
            p.AddContent(string.Empty);
            p.AddContent("<i>" + Local.BodyInfo_BodyInfoToggleHelp.Format("<b>B</b>") +
                         "</i>"); //"Press <<1>> to open this window again"

            // set metadata
            p.Title(Lib.BuildString(Lib.Ellipsis(body.bodyName, Styles.ScaleStringLength(24)), " ",
                Lib.Color(Local.BodyInfo_title, Lib.Kolor.LightGrey))); //"BODY INFO"
        }
    }
}
