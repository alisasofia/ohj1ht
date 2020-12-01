using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;

/// @author  Alisa Karjalainen
/// @version 30.11.2020
///
/// <summary>
/// Peli, jossa tippuvia hedelmiä kerätään laatikkoon. Pelin voittaa kun pelaaja saa 20 pistettä. Pelin häviää jos pelaajan pisteet ovat -5 pistettä.
/// </summary>

public class Hedelmät : PhysicsGame
{
    private PhysicsObject laatikko;

    private IntMeter pelaajanPisteet;

    private PhysicsObject alaReuna;

    /// <summary>
    /// Pääohjelmassa kutsutaan aliohjelmia, joissa luodaan kenttä, lisätään pelaajan pisteiden laskuri ja asetetaan pelikentän ohjaimet.
    /// </summary>
    public override void Begin()
    {
        LuoKentta();
        LisaaLaskurit();
        AsetaOhjaimet();
    }


    /// <summary>
    /// Luodaan pelikenttän pelaajaksi aluksi tyhjä hedelmälaatikko.
    /// Lisätään pelikenttään ajastin, joka arpoo hedelmiä pelikentän yläosaan.
    /// Lisätään kenttään painovoima, pelikentän reunat ja taustan väri.
    /// </summary>
    private void LuoKentta()
    {
        laatikko = PhysicsObject.CreateStaticObject(300.0, 170.0);
        laatikko.Image = LoadImage("LaatikkoTyhja");
        laatikko.X = 0.0;
        laatikko.Y = Level.Bottom + 50.0;
        laatikko.IgnoresGravity = true;
        Add(laatikko);


        Timer timer = new Timer();
        timer.Interval = 1.000;
        timer.Timeout += delegate { RandomGen.SelectOne<Action>(LuoBanaani, LuoMeloni).Invoke(); };
        timer.Start();


        Gravity = new Vector(0.0, -150.0);
        Level.Background.CreateGradient(Color.White, Color.Orange);
        Camera.ZoomToLevel();

        PhysicsObject vasenReuna = Level.CreateLeftBorder();
        vasenReuna.IsVisible = true;

        PhysicsObject oikeaReuna = Level.CreateRightBorder();
        oikeaReuna.IsVisible = true;

        alaReuna = Level.CreateBottomBorder();
        alaReuna.IsVisible = false;

    }


    /// <summary>
    /// Luodaan banaani, jota on tarkoitus kerätä laatikkoon.
    /// Silmukka arpoo montako kappaletta banaaneja kentälle luodaan yhtäaikaa. Banaaneja arvotaan 1-3 kpl.  
    /// </summary>
    private void LuoBanaani()
    {
        PhysicsObject banaani;
        for (int i = 0; i < RandomGen.NextInt(1, 3); i++)
        {
            banaani = new PhysicsObject(70.0, 70.0);
            banaani.Image = LoadImage("Banaani1");
            banaani.X = RandomGen.NextDouble(Level.Left, Level.Right);
            banaani.Y = Level.Top;
            AddCollisionHandler(banaani, KasitteleHedelmanTormays);
            AddCollisionHandler(banaani, KasitteleHedelmanAlaReunanTormays);
            Add(banaani);
        }
    }


    /// <summary>
    /// Luodaan meloni, jota on tarkoitus kerätä laatikkoon.
    /// Silmukka arpoo montako kappaletta meloneja kentälle luodaan yhtäaikaa. Meloneja arvotaan 1-3 kpl.  
    /// </summary>
    private void LuoMeloni()
    {
        PhysicsObject meloni;
        for (int i = 0; i < RandomGen.NextInt(1, 3); i++)
        {
            meloni = new PhysicsObject(70.0, 70.0);
            meloni.Image = LoadImage("Meloni1.0");
            meloni.X = RandomGen.NextDouble(Level.Left, Level.Right);
            meloni.Y = Level.Top;
            AddCollisionHandler(meloni, KasitteleHedelmanTormays);
            AddCollisionHandler(meloni, KasitteleHedelmanAlaReunanTormays);
            Add(meloni);
        }
    }


    /// <summary>
    ///  Lisätään peliin laskuri, joka laskee pelaajan pisteet.
    /// </summary>
    private void LisaaLaskurit()
    {
        pelaajanPisteet = LuoPisteLaskuri(Screen.Right - 100.0, Screen.Top - 100.0);
    }


    /// <summary>
    ///  Luodaan pelikenttään pistelaskuri kentän oikeaan ylänurkkaan.
    /// </summary>
    /// <param name="x">laskurin sijainti x-akselilla</param>
    /// <param name="y">laskurin sijainti y-akselilla</param>
    /// <returns>palauttaa laskurin</returns>
    private IntMeter LuoPisteLaskuri(double x, double y)
    {
        IntMeter laskuri = new IntMeter(0);
        laskuri.MaxValue = 50;
        laskuri.MinValue = -50;

        Label naytto = new Label();
        naytto.BindTo(laskuri);
        naytto.X = x;
        naytto.Y = y;
        naytto.TextColor = Color.Black;
        naytto.BorderColor = Level.Background.Color;
        naytto.Color = Level.Background.Color;
        Add(naytto);
        return laskuri;
    }


    /// <summary>
    /// Tässä aliohjelmassa käsitellään hedelmän putoaminen laatikkoon eli osuminen pelaajaan.
    /// Kun hedelmä osuu pelaajaan, pelaajan pisteet kasvavat yhdellä ja osumisesta kuuluu räjähdys. Hedelmä katoaa pelikentältä ja ikään kuin siirtyy laatikkoon.
    /// Pelaajan kuva vaihtuu tyhjästä laatikosta täydemmäksi kun pelaajan pisteet kasvavat. Pelaajan kuva laatikosta muuttuu tyhjemmäksi, jos pisteet vähenevät.
    /// </summary>
    /// <param name="hedelma">parametri viittaa banaaniin tai meloniin</param>
    /// <param name="kohde">pelaaja eli hedelmälaatikko</param>
    private void KasitteleHedelmanTormays(PhysicsObject hedelma, PhysicsObject kohde)
    {
        Image[] laatikkoKuvat = LoadImages("LaatikkoTyhja", "LaatikkoPuoliksiTaynna", "LaatikkoTaynna");

        if (kohde == laatikko)
        {
            Explosion rajahdys = new Explosion(hedelma.Width);
            rajahdys.Position = hedelma.Position;
            Add(rajahdys);
            hedelma.Destroy();
            pelaajanPisteet.Value ++;
            if (pelaajanPisteet.Value <= 6) laatikko.Image = laatikkoKuvat[0];
            if (pelaajanPisteet.Value > 6) laatikko.Image = laatikkoKuvat[1];
            if (pelaajanPisteet.Value > 15) laatikko.Image = laatikkoKuvat[2];
            LopetaPeli();
        }
    }


    /// <summary>
    /// Tässä aliohjelmassa käsitellään hedelmän putoaminen pelikentän alareunaan. Jos laatikko ei ehdi kerätä hedelmää, hedelmä osuu kentän alareunaan ja tuhoutuu.
    /// Hedelmän osuessa pelikentän alareunaan pelaaja menettää kaksi pistettä.
    /// </summary>
    /// <param name="hedelma"> banaani tai meloni</param>
    /// <param name="alas"></param>
    private void KasitteleHedelmanAlaReunanTormays(PhysicsObject hedelma, PhysicsObject alas)
    {
        if (alas == alaReuna)
        {
            hedelma.Destroy();
            pelaajanPisteet.Value -= 3;
            LopetaPeli();
        }
    }


    /// <summary>
    /// Aliohjelmassa luodaan pelin ohjaimet. Pelaaja liikkuu näppäimistön nuolinäppäimillä oikealle ja vasemmalle.
    /// </summary>
    private void AsetaOhjaimet()
    {
        Vector nopeusOikea = new Vector(500.0, 0.0);
        Vector nopeusVasen = new Vector(-500.0, 0.0);

        Keyboard.Listen(Key.Left, ButtonState.Down, AsetaNopeus, "Laatikko liikkuu vasemmalle", laatikko, nopeusVasen);
        Keyboard.Listen(Key.Left, ButtonState.Released, AsetaNopeus, null, laatikko, Vector.Zero);

        Keyboard.Listen(Key.Right, ButtonState.Down, AsetaNopeus, "Laatikko liikkuu oikealle", laatikko, nopeusOikea);
        Keyboard.Listen(Key.Right, ButtonState.Released, AsetaNopeus, null, laatikko, Vector.Zero);

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
    }


    /// <summary>
    /// Aliojelmassa astetaan rajat pelaajan liikkumiselle pelikentällä.
    /// Jos pelaaja kohtaan kentän oikean reunan oikea nuolinäppäin ei enää liikuta pelaajaa.
    /// Jos pelaaja kohtaa kentän vasemman reunan, vasen nuolinäppäin ei nää liikuta pelajaa.
    /// </summary>
    /// <param name="laatikko">pelaaja</param>
    /// <param name="nopeus">pelaajan sijainti</param>
    private void AsetaNopeus(PhysicsObject laatikko, Vector nopeus)
    {
        if ((nopeus.X < 0) && (laatikko.Left < Level.Left))
        {
            laatikko.Velocity = Vector.Zero;
            return;
        }
        if ((nopeus.X > 0) && (laatikko.Right > Level.Right))
        {
            laatikko.Velocity = Vector.Zero;
            return;
        }

        laatikko.Velocity = nopeus;
    }


    /// <summary>
    /// Aliohjelma lopettaa pelin, jos pelaajan pisteet on 20 tai jos pelaajan pisteet laskee -5 pisteeseen.
    /// Jos pelaajan pisteet ovat 20, pelaaja voittaa pelin.
    /// Jos pelaajan pisteet ovat -5, pelaaja häviää pelin.
    /// </summary>
    private void LopetaPeli()
    {
        if (pelaajanPisteet.Value >= 20)
        {
            MessageDisplay.Add("Voitit pelin!");
            Gravity = Vector.Zero;
            StopAll();
            Keyboard.Disable(Key.Right);
            Keyboard.Disable(Key.Left);
        }

        if (pelaajanPisteet.Value <= -5)
        {
            MessageDisplay.Add("Hävisit pelin!");
            Gravity = Vector.Zero;
            StopAll();
            Keyboard.Disable(Key.Right);
            Keyboard.Disable(Key.Left);
        }
    }
}


