using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;

/// @author  Alisa Karjalainen
/// @version 01.12.2020
///
/// <summary>
/// Peli, jossa tippuvia hedelmiä kerätään laatikkoon. Pelin voittaa kun pelaaja saa 20 pistettä. Pelin häviää jos pelaajan pisteet ovat -5 pistettä.
/// </summary>

public class Hedelmät : PhysicsGame
{
    private PhysicsObject laatikko;
    private IntMeter pelaajanPisteet;
    private static readonly Image[] laatikkoKuvat = LoadImages("LaatikkoTyhja", "LaatikkoPuoliksiTaynna", "LaatikkoTaynna");

    private const int MAX_HEDELMIEN_MAARA = 3;
    private const int MIN_HEDELMIEN_MAARA = 1;
    private const double NOPEUS = 500.0;
    private const double PYSAHTYY = 0.0;
    private const double POHJAKOKO = 70.0;
    private const double LAATIKKO_Y = 50.0;
    private const double LAATIKKO_X = 0.0;
    private const double SEKUNTI = 1.0;
    private const double PAINOVOIMA = 150;
    private const double LASKURI_SIJAINTI = 100.0;
    private const int PISTEET = 50;
    private const int PISTE_RAJA = 6;

    /// <summary>
    /// Pääohjelmassa kutsutaan aliohjelmia, joissa luodaan kenttä, lisätään pelaajan pisteiden laskuri ja asetetaan pelikentän ohjaimet.
    /// </summary>
    public override void Begin()
    {
        ClearAll();
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
        laatikko = PhysicsObject.CreateStaticObject(3*POHJAKOKO, 2*POHJAKOKO);
        laatikko.Image = laatikkoKuvat[0];
        laatikko.X = LAATIKKO_X;
        laatikko.Y = Level.Bottom + LAATIKKO_Y;
        laatikko.IgnoresGravity = true;
        Add(laatikko);


        Timer timer = new Timer();
        timer.Interval = SEKUNTI;
        timer.Timeout += delegate { RandomGen.SelectOne<Action>(LuoBanaani, LuoMeloni).Invoke(); };
        timer.Start();


        Gravity = new Vector(PYSAHTYY, -PAINOVOIMA);
        Level.Background.CreateGradient(Color.White, Color.Orange);
        Camera.ZoomToLevel();

        PhysicsObject vasenReuna = Level.CreateLeftBorder();
        vasenReuna.IsVisible = true;

        PhysicsObject oikeaReuna = Level.CreateRightBorder();
        oikeaReuna.IsVisible = true;


        PhysicsObject alaReuna = Level.CreateBottomBorder();
        alaReuna.IsVisible = false;
        alaReuna.Tag = "alareuna";
    }


    /// <summary>
    /// Luodaan banaani, jota on tarkoitus kerätä laatikkoon.
    /// </summary>
    private void LuoBanaani()
    {
            LuoHedelma(LoadImage("Banaani1"));
    }


    /// <summary>
    /// Luodaan meloni, jota on tarkoitus kerätä laatikkoon.
    /// </summary>
    private void LuoMeloni()
    {
        LuoHedelma(LoadImage("Meloni1.0"));
    }


    /// <summary>
    /// Silmukka arpoo montako kappaletta hedelmiä kentälle luodaan yhtäaikaa. Hedelmiä arvotaan 1-3 kpl.  
    /// </summary>
    /// <param name="kuva">banaanin tai hedelmän kuva</param>
    private void LuoHedelma(Image kuva)
    {
        PhysicsObject hedelma;
        for (int i = 0; i < RandomGen.NextInt(MIN_HEDELMIEN_MAARA, MAX_HEDELMIEN_MAARA); i++)
        {
            hedelma = new PhysicsObject(POHJAKOKO, POHJAKOKO);
            hedelma.Image = kuva;
            hedelma.X = RandomGen.NextDouble(Level.Left, Level.Right);
            hedelma.Y = Level.Top;
            AddCollisionHandler(hedelma, KasitteleHedelmanTormays);
            AddCollisionHandler(hedelma, "alareuna", KasitteleHedelmanAlaReunanTormays);
            Add(hedelma);
        }
    }

    /// <summary>
    ///  Lisätään peliin laskuri, joka laskee pelaajan pisteet.
    /// </summary>
    private void LisaaLaskurit()
    {
        pelaajanPisteet = LuoPisteLaskuri(Screen.Right - LASKURI_SIJAINTI, Screen.Top - LASKURI_SIJAINTI);
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
        laskuri.MaxValue = PISTEET;
        laskuri.MinValue = -PISTEET;

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
        if (kohde == laatikko)
        {
            Explosion rajahdys = new Explosion(hedelma.Width);
            rajahdys.Position = hedelma.Position;
            Add(rajahdys);
            hedelma.Destroy();
            pelaajanPisteet.Value ++;
            if (pelaajanPisteet.Value <= PISTE_RAJA) laatikko.Image = laatikkoKuvat[0];
            if (pelaajanPisteet.Value > PISTE_RAJA) laatikko.Image = laatikkoKuvat[1];
            if (pelaajanPisteet.Value > 2*PISTE_RAJA) laatikko.Image = laatikkoKuvat[2];
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
        // if (alas == alaReuna)
        {
            hedelma.Destroy();
            pelaajanPisteet.Value -= PISTE_RAJA/2;
            LopetaPeli();
        }
    }


    /// <summary>
    /// Aliohjelmassa luodaan pelin ohjaimet. Pelaaja liikkuu näppäimistön nuolinäppäimillä oikealle ja vasemmalle.
    /// </summary>
    private void AsetaOhjaimet()
    {
        Vector nopeusOikea = new Vector(NOPEUS, PYSAHTYY);
        Vector nopeusVasen = new Vector(-NOPEUS, PYSAHTYY);

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
        if (pelaajanPisteet.Value >= (PISTE_RAJA*3)+2)
        {
            LoppuTulos("Voitit pelin!");
        }

        if (pelaajanPisteet.Value <= -PISTE_RAJA)
        {
            LoppuTulos("Hävisit pelin!");
        }
    }


    /// <summary>
    /// Pelin loppumisen jälkeen kentältä katoaa kaikki ja 4 sekuntin kuluttua peli alkaa alusta.
    /// </summary>
    /// <param name="viesti">Viestii päättyikö peli voittoon vai häviöön</param>
    private void LoppuTulos(String viesti)
    {
        MessageDisplay.Add(viesti);
        Gravity = Vector.Zero;
        StopAll();
        Keyboard.Disable(Key.Right);
        Keyboard.Disable(Key.Left);
        Timer.SingleShot(3, Begin);
    }
}


