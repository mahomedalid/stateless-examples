using System;
using Microsoft.EntityFrameworkCore;
using TelephoneCallExample;

using (var dbContext = new AppDbContext())
{
    dbContext.Database.EnsureCreated();

}

using (var dbContext = new AppDbContext())
{
    var phoneCall = new PhoneCallSaga("Maho", dbContext);

    phoneCall.Print();
    phoneCall.Dialed("HQ");
    phoneCall.Print();
    phoneCall.Connected();
    phoneCall.Print();
    phoneCall.SetVolume(2);
    phoneCall.Print();
    phoneCall.Hold();
    phoneCall.Print();
    phoneCall.Mute();
    phoneCall.Print();
    phoneCall.Unmute();
    phoneCall.Print();
    phoneCall.Resume();
    phoneCall.Print();
    phoneCall.SetVolume(11);
    phoneCall.Print();

    Console.WriteLine(phoneCall.ToDotGraph());

    Console.WriteLine("Press any key...");
    Console.ReadKey(true);
};

    