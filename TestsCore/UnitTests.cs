﻿using System;
using System.Linq;
using System.Threading.Tasks;
#if NET40
using NUnit.Framework;
using Fact = NUnit.Framework.TestAttribute;
#else
using Xunit;
#endif

#if EF_CORE
using Microsoft.EntityFrameworkCore;
namespace EntityFrameworkCore.Triggers.Tests {
#else
using System.Data.Entity;
using System.Data.Entity.Validation;
namespace EntityFramework.Triggers.Tests {
#endif

	public class UnitTests {
		public static void Main(String[] args) { }
		// inserting
		// insertfailed
		// inserted
		// updating
		// updatefailed
		// updated
		// deleting
		// deletefailed
		// deleted

		// cancel inserting
		// cancel updating
		// cancel deleting

		// DbEntityValidationException
		// DbUpdateException

		// event order
		// inheritance hierarchy event order

		// original values on updating
		// firing 'before' triggers of an entity added by another's "before" trigger, all before actual SaveChanges is executed

		// Cancelled property of "before" trigger
		// Swallow proprety of "failed" trigger

		// TODO:
		// event loops
		// calling savechanges in an event handler
		// doubly-declared interfaces

		// test ...edFailed exception logic...
		//     DbUpdateException raises failed triggers and is swallowable if contains entries or changetracker has only one entry
		//     DbEntityValidationException raises failed triggers and is swallowable if it contains entries
		//     All other exceptions raises failed triggers and is swallowable if changetracker has only one entry
	}

	public class AddRemoveEventHandler : TestBase {
		protected override void Setup() => Triggers<Thing, Context>.Inserting += TriggersOnInserting;
		protected override void Teardown() => Triggers<Thing, Context>.Inserting -= TriggersOnInserting;

		private Int32 triggerCount;
		private void TriggersOnInserting(IBeforeEntry<Thing> beforeEntry) => ++triggerCount;

		[Fact]
		public void Sync() => DoATest(() => {
			Context.Things.Add(new Thing { Value = "Foo" });
			Context.SaveChanges();
			Assert.True(1 == triggerCount);

			Teardown(); // Remove handler
			Context.Things.Add(new Thing { Value = "Foo" });
			Context.SaveChanges();
			Assert.True(1 == triggerCount);
		});

#if !NET40
		[Fact]
		public Task Async() => DoATestAsync(async () => {
			Context.Things.Add(new Thing { Value = "Foo" });
			await Context.SaveChangesAsync().ConfigureAwait(false);
			Assert.True(1 == triggerCount);

			Teardown(); // Remove handler
			Context.Things.Add(new Thing { Value = "Foo" });
			await Context.SaveChangesAsync().ConfigureAwait(false);
			Assert.True(1 == triggerCount);
		});
#endif
	}

	public class Insert : ThingTestBase {
		[Fact]
		public void Sync() => DoATest(() => {
			var guid = Guid.NewGuid().ToString();
			var thing = new Thing { Value = guid };
			Context.Things.Add(thing);
			Context.SaveChanges();
			InsertedCheckFlags(thing);
			Assert.True(Context.Things.SingleOrDefault(x => x.Value == guid) != null);
		});

#if !NET40
		[Fact]
		public Task Async() => DoATestAsync(async () => {
			var guid = Guid.NewGuid().ToString();
			var thing = new Thing { Value = guid };
			Context.Things.Add(thing);
			await Context.SaveChangesAsync().ConfigureAwait(false);
			InsertedCheckFlags(thing);
			Assert.True(await Context.Things.SingleOrDefaultAsync(x => x.Value == guid).ConfigureAwait(false) != null);
		});
#endif
	}

	public class InsertFail : ThingTestBase {
		[Fact]
		public void Sync() => DoATest(() => {
			var thing = new Thing { Value = null };
			Context.Things.Add(thing);
			try {
				Context.SaveChanges();
			}
#if EF_CORE
			catch (DbUpdateException) {
#else
			catch (DbEntityValidationException) {
#endif
				InsertFailedCheckFlags(thing);
				return;
			}
			Assert.True(false, "Exception not caught");
		});

#if !NET40
		[Fact]
		public Task Async() => DoATestAsync(async () => {
			var thing = new Thing { Value = null };
			Context.Things.Add(thing);
			try {
				await Context.SaveChangesAsync().ConfigureAwait(false);
			}
#if EF_CORE
			catch (DbUpdateException) {
#else
			catch (DbEntityValidationException) {
#endif
				InsertFailedCheckFlags(thing);
				return;
			}
			Assert.True(false, "Exception not caught");
		});
#endif
	}

	public class InsertFailSwallow : TestBase {
		protected override void Setup() => Triggers<Thing>.InsertFailed += OnInsertFailed;
		protected override void Teardown() => Triggers<Thing>.InsertFailed -= OnInsertFailed;

		private static void OnInsertFailed(IFailedEntry<Thing, DbContext> e) {
#if EF_CORE
			Assert.True(e.Exception is DbUpdateException);
#else
			Assert.True(e.Exception is DbEntityValidationException);
#endif
			e.Swallow = true;
		}

		[Fact]
		public void Sync() => DoATest(() => {
			Context.Things.Add(new Thing { Value = null });
			Context.SaveChanges();
		});

#if !NET40
		[Fact]
		public Task Async() => DoATestAsync(async () => {
			Context.Things.Add(new Thing { Value = null });
			await Context.SaveChangesAsync().ConfigureAwait(false);
		});
#endif
	}

	public class Update : ThingTestBase {
		[Fact]
		public void Sync() => DoATest(() => {
			var thing = new Thing { Value = "Foo" };
			Context.Things.Add(thing);
			Context.SaveChanges();
			thing.Value = "Bar";
			ResetFlags(thing);
			Context.SaveChanges();
			UpdatedCheckFlags(thing);
		});

#if !NET40
		[Fact]
		public Task Async() => DoATestAsync(async () => {
			var thing = new Thing { Value = "Foo" };
			Context.Things.Add(thing);
			await Context.SaveChangesAsync().ConfigureAwait(false);
			thing.Value = "Bar";
			ResetFlags(thing);
			await Context.SaveChangesAsync().ConfigureAwait(false);
			UpdatedCheckFlags(thing);
		});
#endif
	}

	public class UpdateFail : ThingTestBase {
		[Fact]
		public void Sync() => DoATest(() => {
			var thing = new Thing { Value = "Foo" };
			Context.Things.Add(thing);
			Context.SaveChanges();
			thing.Value = null;
			ResetFlags(thing);
			try {
				Context.SaveChanges();
			}
#if EF_CORE
			catch (DbUpdateException) {
#else
			catch (DbEntityValidationException) {
#endif
				UpdateFailedCheckFlags(thing);
				return;
			}
			Assert.True(false, "Exception not caught");
		});

#if !NET40
		[Fact]
		public Task Async() => DoATestAsync(async () => {
			var thing = new Thing { Value = "Foo" };
			Context.Things.Add(thing);
			await Context.SaveChangesAsync().ConfigureAwait(false);
			thing.Value = null;
			ResetFlags(thing);
			try {
				await Context.SaveChangesAsync().ConfigureAwait(false);
			}
#if EF_CORE
			catch (DbUpdateException) {
#else
			catch (DbEntityValidationException) {
#endif
				UpdateFailedCheckFlags(thing);
				return;
			}
			Assert.True(false, "Exception not caught");
		});
#endif
	}

	public class UpdateFailSwallow : TestBase {
		protected override void Setup() => Triggers<Thing>.UpdateFailed += OnUpdateFailed;
		protected override void Teardown() => Triggers<Thing>.UpdateFailed -= OnUpdateFailed;

		private static void OnUpdateFailed(IFailedEntry<Thing, DbContext> e) {
#if EF_CORE
			Assert.True(e.Exception is DbUpdateException);
#else
			Assert.True(e.Exception is DbEntityValidationException);
#endif
			e.Swallow = true;
		}

		[Fact]
		public void Sync() => DoATest(() => {
			var thing = new Thing { Value = "Foo" };
			Context.Things.Add(thing);
			Context.SaveChanges();
			thing.Value = null;
			Context.SaveChanges();
		});

#if !NET40
		[Fact]
		public Task Async() => DoATestAsync(async () => {
			var thing = new Thing { Value = "Foo" };
			Context.Things.Add(thing);
			await Context.SaveChangesAsync().ConfigureAwait(false);
			thing.Value = null;
			await Context.SaveChangesAsync().ConfigureAwait(false);
		});
#endif
	}

	public class Delete : ThingTestBase {
		[Fact]
		public void Sync() => DoATest(() => {
			var thing = new Thing { Value = "Foo" };
			Context.Things.Add(thing);
			Context.SaveChanges();
			ResetFlags(thing);
			Context.Things.Remove(thing);
			Context.SaveChanges();
			DeletedCheckFlags(thing);
		});

#if !NET40
		[Fact]
		public Task Async() => DoATestAsync(async () => {
			var thing = new Thing { Value = "Foo" };
			Context.Things.Add(thing);
			await Context.SaveChangesAsync().ConfigureAwait(false);
			ResetFlags(thing);
			Context.Things.Remove(thing);
			await Context.SaveChangesAsync().ConfigureAwait(false);
			DeletedCheckFlags(thing);
		});
#endif
	}

	public class DeleteFail : ThingTestBase {
		protected override void Setup() {
			base.Setup();
			Triggers<Thing>.Deleting += OnDeleting;
		}

		protected override void Teardown() {
			Triggers<Thing>.Deleting -= OnDeleting;
			base.Teardown();
		}

		private static void OnDeleting(IBeforeChangeEntry<Thing, DbContext> e) {
			throw new Exception();
		}

		[Fact]
		public void Sync() => DoATest(() => {
			var thing = new Thing { Value = "Foo" };
			Context.Things.Add(thing);
			Context.SaveChanges();
			ResetFlags(thing);
			Context.Things.Remove(thing);
			try {
				Context.SaveChanges();
			}
			catch (Exception) {
				DeleteFailedCheckFlags(thing);
				return;
			}
			Assert.True(false, "Exception not caught");
		});

#if !NET40
		[Fact]
		public Task Async() => DoATestAsync(async () => {
			var thing = new Thing { Value = "Foo" };
			Context.Things.Add(thing);
			await Context.SaveChangesAsync().ConfigureAwait(false);
			ResetFlags(thing);
			Context.Things.Remove(thing);
			try {
				await Context.SaveChangesAsync().ConfigureAwait(false);
			}
			catch (Exception) {
				DeleteFailedCheckFlags(thing);
				return;
			}
			Assert.True(false, "Exception not caught");
		});
#endif
	}

	public class DeleteFailSwallow : TestBase {
		protected override void Setup() {
			Triggers<Thing>.Deleting += OnDeleting;
			Triggers<Thing>.DeleteFailed += OnDeleteFailed;
		}

		protected override void Teardown() {
			Triggers<Thing>.DeleteFailed -= OnDeleteFailed;
			Triggers<Thing>.Deleting -= OnDeleting;
		}

		private static void OnDeleting(IBeforeChangeEntry<Thing, DbContext> e) {
			throw new Exception();
		}

		private static void OnDeleteFailed(IChangeFailedEntry<Thing, DbContext> e) {
			e.Swallow = true;
		}

		[Fact]
		public void Sync() => DoATest(() => {
			var thing = new Thing { Value = "Foo" };
			Context.Things.Add(thing);
			Context.SaveChanges();
			Context.Things.Remove(thing);
			Context.SaveChanges();
		});

#if !NET40
		[Fact]
		public Task Async() => DoATestAsync(async () => {
			var thing = new Thing { Value = "Foo" };
			Context.Things.Add(thing);
			await Context.SaveChangesAsync().ConfigureAwait(false);
			Context.Things.Remove(thing);
			await Context.SaveChangesAsync().ConfigureAwait(false);
		});
#endif
	}

	public class InsertingCancel : ThingTestBase {
		protected override void Setup() {
			base.Setup();
			Triggers<Thing>.Inserting += Cancel;
		}
		protected override void Teardown() {
			Triggers<Thing>.Inserting -= Cancel;
			base.Teardown();
		}

		protected virtual void Cancel(IBeforeEntry<Thing> e) => e.Cancel();

		[Fact]
		public void Sync() => DoATest(() => {
			var guid = Guid.NewGuid().ToString();
			var thing = new Thing { Value = guid };
			Context.Things.Add(thing);
			Context.SaveChanges();
			InsertingCheckFlags(thing);
			Assert.True(Context.Things.SingleOrDefault(x => x.Value == guid) == null);
		});

#if !NET40
		[Fact]
		public Task Async() => DoATestAsync(async () => {
			var guid = Guid.NewGuid().ToString();
			var thing = new Thing { Value = guid };
			Context.Things.Add(thing);
			await Context.SaveChangesAsync().ConfigureAwait(false);
			InsertingCheckFlags(thing);
			Assert.True(await Context.Things.SingleOrDefaultAsync(x => x.Value == guid).ConfigureAwait(false) == null);
		});
#endif
	}

	public class InsertingCancelledTrue : InsertingCancel {
		protected override void Cancel(IBeforeEntry<Thing> e) => e.Cancelled = true;

		[Fact]
		public new void Sync() => base.Sync();

#if !NET40
		[Fact]
		public new Task Async() => base.Async();
#endif
	}

	public class UpdatingCancel : ThingTestBase {
		protected override void Setup() {
			base.Setup();
			Triggers<Thing>.Updating += Cancel;
		}
		protected override void Teardown() {
			Triggers<Thing>.Updating -= Cancel;
			base.Teardown();
		}

		protected virtual void Cancel(IBeforeChangeEntry<Thing, DbContext> e) => e.Cancel();

		[Fact]
		public void Sync() => DoATest(() => {
			var guid = Guid.NewGuid().ToString();
			var thing = new Thing { Value = guid };
			Context.Things.Add(thing);
			Context.SaveChanges();
			ResetFlags(thing);
			var updatedGuid = Guid.NewGuid().ToString();
			thing.Value = updatedGuid;
			Context.SaveChanges();
			UpdatingCheckFlags(thing);
			Assert.True(Context.Things.SingleOrDefault(x => x.Value == guid) != null);
			Assert.True(Context.Things.SingleOrDefault(x => x.Value == updatedGuid) == null);
		});

#if !NET40
		[Fact]
		public Task Async() => DoATestAsync(async () => {
			var guid = Guid.NewGuid().ToString();
			var thing = new Thing { Value = guid };
			Context.Things.Add(thing);
			Context.SaveChanges();
			ResetFlags(thing);
			var updatedGuid = Guid.NewGuid().ToString();
			thing.Value = updatedGuid;
			await Context.SaveChangesAsync();
			UpdatingCheckFlags(thing);
			Assert.True(await Context.Things.SingleOrDefaultAsync(x => x.Value == guid).ConfigureAwait(false) != null);
			Assert.True(await Context.Things.SingleOrDefaultAsync(x => x.Value == updatedGuid).ConfigureAwait(false) == null);
		});
#endif
	}

	public class UpdatingCancelledTrue : UpdatingCancel {
		protected override void Cancel(IBeforeChangeEntry<Thing, DbContext> e) => e.Cancelled = true;

		[Fact]
		public new void Sync() => base.Sync();

#if !NET40
		[Fact]
		public new Task Async() => base.Async();
#endif
	}

	public class DeletingCancel : ThingTestBase {
		protected override void Setup() {
			base.Setup();
			Triggers<Thing>.Deleting += Cancel;
		}
		protected override void Teardown() {
			Triggers<Thing>.Deleting -= Cancel;
			base.Teardown();
		}

		protected virtual void Cancel(IBeforeChangeEntry<Thing, DbContext> e) => e.Cancel();

		[Fact]
		public void Sync() => DoATest(() => {
			var guid = Guid.NewGuid().ToString();
			var thing = new Thing { Value = guid };
			Context.Things.Add(thing);
			Context.SaveChanges();
			ResetFlags(thing);
			Context.Things.Remove(thing);
			Context.SaveChanges();
			DeletingCheckFlags(thing);
			Assert.True(Context.Things.SingleOrDefault(x => x.Value == guid) != null);
		});

#if !NET40
		[Fact]
		public Task Async() => DoATestAsync(async () => {
			var guid = Guid.NewGuid().ToString();
			var thing = new Thing { Value = guid };
			Context.Things.Add(thing);
			await Context.SaveChangesAsync();
			ResetFlags(thing);
			Context.Things.Remove(thing);
			await Context.SaveChangesAsync();
			DeletingCheckFlags(thing);
			Assert.True(await Context.Things.SingleOrDefaultAsync(x => x.Value == guid).ConfigureAwait(false) != null);
		});
#endif
	}

	public class DeletingCancelledTrue : DeletingCancel {
		protected override void Cancel(IBeforeChangeEntry<Thing, DbContext> e) => e.Cancelled = true;

		[Fact]
		public new void Sync() => base.Sync();

#if !NET40
		[Fact]
		public new Task Async() => base.Async();
#endif
	}

	public class EventFiringOrderRelativeToAttachment : TestBase {
		protected override void Setup() {
			Triggers<Thing>.Inserting += Add1;
			Triggers<Thing>.Inserting += Add2;
			Triggers<Thing>.Inserting += Add3;
		}

		protected override void Teardown() {
			Triggers<Thing>.Inserting -= Add1;
			Triggers<Thing>.Inserting -= Add2;
			Triggers<Thing>.Inserting -= Add3;
		}

		private static void Add1(IBeforeEntry<Thing, DbContext> e) => e.Entity.Numbers.Add(1);
		private static void Add2(IBeforeEntry<Thing, DbContext> e) => e.Entity.Numbers.Add(2);
		private static void Add3(IBeforeEntry<Thing, DbContext> e) => e.Entity.Numbers.Add(3);
		
		[Fact]
		public void Sync() => DoATest(() => {
			var thing = new Thing { Value = Guid.NewGuid().ToString() };
			Context.Things.Add(thing);
			Context.SaveChanges();
			Assert.True(thing.Numbers.SequenceEqual(new [] { 1, 2, 3 }));
		});

#if !NET40
		[Fact]
		public Task Async() => DoATestAsync(async () => {
			var thing = new Thing { Value = Guid.NewGuid().ToString() };
			Context.Things.Add(thing);
			await Context.SaveChangesAsync().ConfigureAwait(false);
			Assert.True(thing.Numbers.SequenceEqual(new[] { 1, 2, 3 }));
		});
#endif
	}

	public class EventFiringOrderRelativeToClassHierarchy : TestBase {
		protected override void Setup() {
			Triggers<RoyalGala>.Inserting += Add3;
			Triggers<Apple>.Inserting     += Add2;
			Triggers<Thing>.Inserting     += Add1;
		}

		protected override void Teardown() {
			Triggers<Thing>.Inserting     -= Add1;
			Triggers<Apple>.Inserting     -= Add2;
			Triggers<RoyalGala>.Inserting -= Add3;
		}

		private static void Add1(IBeforeEntry<Thing, DbContext> e) => e.Entity.Numbers.Add(1);
		private static void Add2(IBeforeEntry<Thing, DbContext> e) => e.Entity.Numbers.Add(2);
		private static void Add3(IBeforeEntry<Thing, DbContext> e) => e.Entity.Numbers.Add(3);

		[Fact]
		public void Sync() => DoATest(() => {
			var royalGala = new RoyalGala { Value = Guid.NewGuid().ToString() };
			Context.RoyalGalas.Add(royalGala);
			Context.SaveChanges();
			Assert.True(royalGala.Numbers.SequenceEqual(new[] { 1, 2, 3 }));
		});

#if !NET40
		[Fact]
		public Task Async() => DoATestAsync(async () => {
			var royalGala = new RoyalGala { Value = Guid.NewGuid().ToString() };
			Context.RoyalGalas.Add(royalGala);
			await Context.SaveChangesAsync().ConfigureAwait(false);
			Assert.True(royalGala.Numbers.SequenceEqual(new[] { 1, 2, 3 }));
		});
#endif
	}

	public class OriginalValuesOnUpdating : TestBase {
		protected override void Setup()    => Triggers<Thing>.Updating += TriggersOnUpdating;
		protected override void Teardown() => Triggers<Thing>.Updating -= TriggersOnUpdating;

		private void TriggersOnUpdating(IBeforeChangeEntry<Thing, DbContext> beforeChangeEntry) {
			Assert.True(beforeChangeEntry.Original.Value == guid);
			Assert.True(beforeChangeEntry.Entity.Value == guid2);
		}

		private String guid;
		private String guid2;

		[Fact]
		public void Sync() => DoATest(() => {
			guid = Guid.NewGuid().ToString();
			guid2 = Guid.NewGuid().ToString();
			var thing = new Thing { Value = guid };
			Context.Things.Add(thing);
			Context.SaveChanges();
			thing.Value = guid2;
			Context.SaveChanges();
		});

#if !NET40
		[Fact]
		public Task Async() => DoATestAsync(async () => {
			guid = Guid.NewGuid().ToString();
			guid2 = Guid.NewGuid().ToString();
			var thing = new Thing { Value = guid };
			Context.Things.Add(thing);
			await Context.SaveChangesAsync().ConfigureAwait(false);
			thing.Value = guid2;
			await Context.SaveChangesAsync().ConfigureAwait(false);
		});
#endif
	}

	public class OriginalValuesOnDeleting : TestBase {
		protected override void Setup()    => Triggers<Thing>.Deleting += TriggersOnDeleting;
		protected override void Teardown() => Triggers<Thing>.Deleting -= TriggersOnDeleting;

		private void TriggersOnDeleting(IBeforeChangeEntry<Thing, DbContext> beforeChangeEntry) {
			Assert.True(beforeChangeEntry.Original.Value == guid);
			Assert.True(beforeChangeEntry.Entity.Value == guid2);
		}

		private String guid;
		private String guid2;

		[Fact]
		public void Sync() => DoATest(() => {
			guid = Guid.NewGuid().ToString();
			guid2 = Guid.NewGuid().ToString();
			var thing = new Thing { Value = guid };
			Context.Things.Add(thing);
			Context.SaveChanges();
			thing.Value = guid2;
			Context.Things.Remove(thing);
			Context.SaveChanges();
		});

#if !NET40
		[Fact]
		public Task Async() => DoATestAsync(async () => {
			guid = Guid.NewGuid().ToString();
			guid2 = Guid.NewGuid().ToString();
			var thing = new Thing { Value = guid };
			Context.Things.Add(thing);
			await Context.SaveChangesAsync().ConfigureAwait(false);
			thing.Value = guid2;
			Context.Things.Remove(thing);
			await Context.SaveChangesAsync().ConfigureAwait(false);
		});
#endif
	}

	public class Covariance : TestBase {
		protected override void Setup() {
			Action<IBeforeEntry<Thing, Context>> triggersOnInserting = entry => { }; // These two will break at runtime without the `CoContra` event-backing-field
			Action<IBeforeEntry<Thing, DbContext>> triggersOnInserting2 = entry => { };
			Triggers<Thing, Context>.Inserting += triggersOnInserting;
			Triggers<Thing, Context>.Inserting += triggersOnInserting2;
			Triggers<Thing, Context>.Inserting += ObjectInserting6;
			Triggers<Thing, Context>.Inserting += ObjectInserting5;
			Triggers<Thing, Context>.Inserting += ObjectInserting4;
			Triggers<Thing, Context>.Inserting += ObjectInserting3;
			Triggers<Thing, Context>.Inserting += ObjectInserting2;
			Triggers<Thing, Context>.Inserting += ObjectInserting;
			Triggers<Thing, Context>.Inserting += ThingInserting6;
			Triggers<Thing, Context>.Inserting += ThingInserting5;
			Triggers<Thing, Context>.Inserting += ThingInserting4;
			Triggers<Thing, Context>.Inserting += ThingInserting3;
			Triggers<Thing, Context>.Inserting += ThingInserting2;
			Triggers<Thing, Context>.Inserting += ThingInserting;
		}

		protected override void Teardown() {
			Triggers<Thing, Context>.Inserting += ThingInserting;
			Triggers<Thing, Context>.Inserting += ThingInserting2;
			Triggers<Thing, Context>.Inserting += ThingInserting3;
			Triggers<Thing, Context>.Inserting += ThingInserting4;
			Triggers<Thing, Context>.Inserting += ThingInserting5;
			Triggers<Thing, Context>.Inserting += ThingInserting6;
			Triggers<Thing, Context>.Inserting += ObjectInserting;
			Triggers<Thing, Context>.Inserting += ObjectInserting2;
			Triggers<Thing, Context>.Inserting += ObjectInserting3;
			Triggers<Thing, Context>.Inserting += ObjectInserting4;
			Triggers<Thing, Context>.Inserting += ObjectInserting5;
			Triggers<Thing, Context>.Inserting += ObjectInserting6;
		}

		private Boolean thingInsertingRan;
		private Boolean thingInserting2Ran;
		private Boolean thingInserting3Ran;
		private Boolean objectInsertingRan;
		private Boolean objectInserting2Ran;
		private Boolean objectInserting3Ran;

		private void ThingInserting(IBeforeEntry<Thing, Context> entry) => thingInsertingRan = true;
		private void ThingInserting2(IBeforeEntry<Thing, DbContext> entry) => thingInserting2Ran = true;
		private void ThingInserting3(IBeforeEntry<Thing> entry) => thingInserting3Ran = true;
		private void ThingInserting4(IEntry<Thing, Context> entry)   {}
		private void ThingInserting5(IEntry<Thing, DbContext> entry) {}
		private void ThingInserting6(IEntry<Thing> entry)            {}
		private void ObjectInserting(IBeforeEntry<Object, Context> beforeEntry) => objectInsertingRan = true;
		private void ObjectInserting2(IBeforeEntry<Object, DbContext> beforeEntry) => objectInserting2Ran = true;
		private void ObjectInserting3(IBeforeEntry<Object> beforeEntry) => objectInserting3Ran = true;
		private void ObjectInserting4(IEntry<Object, Context> beforeEntry)   {}
		private void ObjectInserting5(IEntry<Object, DbContext> beforeEntry) {}
		private void ObjectInserting6(IEntry<Object> beforeEntry)            {}

		[Fact]
		public void Sync() => DoATest(() => {
			var guid = Guid.NewGuid().ToString();
			var thing = new Thing { Value = guid };
			Context.Things.Add(thing);
			Assert.False(thingInsertingRan);
			Assert.False(thingInserting2Ran);
			Assert.False(thingInserting3Ran);
			Assert.False(objectInsertingRan);
			Assert.False(objectInserting2Ran);
			Assert.False(objectInserting3Ran);
			Context.SaveChanges();
			Assert.True(Context.Things.SingleOrDefault(x => x.Value == guid) != null);
			Assert.True(thingInsertingRan);
			Assert.True(thingInserting2Ran);
			Assert.True(thingInserting3Ran);
			Assert.True(objectInsertingRan);
			Assert.True(objectInserting2Ran);
			Assert.True(objectInserting3Ran);
		});

#if !NET40
		[Fact]
		public Task Async() => DoATestAsync(async () => {
			var guid = Guid.NewGuid().ToString();
			var thing = new Thing { Value = guid };
			Context.Things.Add(thing);
			Assert.False(thingInsertingRan);
			Assert.False(thingInserting2Ran);
			Assert.False(thingInserting3Ran);
			Assert.False(objectInsertingRan);
			Assert.False(objectInserting2Ran);
			Assert.False(objectInserting3Ran);
			await Context.SaveChangesAsync().ConfigureAwait(false);
			Assert.True(await Context.Things.SingleOrDefaultAsync(x => x.Value == guid).ConfigureAwait(false) != null);
			Assert.True(thingInsertingRan);
			Assert.True(thingInserting2Ran);
			Assert.True(thingInserting3Ran);
			Assert.True(objectInsertingRan);
			Assert.True(objectInserting2Ran);
			Assert.True(objectInserting3Ran);
		});
#endif
	}

	//	public class MultiplyDeclaredInterfaces : TestBase {
	//		protected override void Setup() {}
	//		protected override void Teardown() { }

	//		[Fact]
	//		public void Sync() => DoATest(() => {
	//		});

	//#if !NET40
	//		[Fact]
	//		public Task Async() => DoATestAsync(async () => {
	//		});
	//#endif
	//	}

	//	public interface ICreature { }
	//	public class Creature : ICreature {
	//		[Key]
	//		public virtual Int64 Id { get; protected set; }
	//		public virtual String Name { get; set; }
	//	}
	//	public class Dog : Creature, ICreature { }
}