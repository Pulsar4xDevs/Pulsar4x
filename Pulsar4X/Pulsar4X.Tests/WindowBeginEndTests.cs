using System;
using NUnit.Framework;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// The Window wrapper's nest tracking, without opening an ImGui window.
    /// A skipped End after Begin (for example when a window is collapsed and
    /// Begin returns false) is what threw InvalidOperationException in GameLog.
    /// </summary>
    public class WindowBeginEndTests
    {
        [SetUp]
        public void Reset() => Window.DebugReset();

        [TearDown]
        public void TearDown() => Window.DebugReset();

        [Test]
        public void BeginWithoutEnd_ThrowsWhenTheNextWindowBegins()
        {
            // Collapsing GameLog makes ImGui.Begin return false. The old GameLog path
            // skipped Window.End(), so the next window (an EntityCard) failed here.
            Window.DebugEnterBegin("GameLog");
            Assert.That(Window.DebugBeginCount, Is.EqualTo(1));

            var ex = Assert.Throws<InvalidOperationException>(
                () => Window.DebugEnterBegin("Unknown (Unknown)###1055"));
            Assert.That(ex.Message, Does.Contain("GameLog"));
            Assert.That(ex.Message, Does.Contain("Unknown (Unknown)###1055"));
            Assert.That(ex.Message, Does.Contain("Must call End()"));
        }

        [Test]
        public void EndAfterBegin_AllowsTheNextWindowToBegin()
        {
            Window.DebugEnterBegin("GameLog");
            Window.DebugLeaveEnd();
            Assert.That(Window.DebugBeginCount, Is.EqualTo(0));

            Window.DebugEnterBegin("Unknown (Unknown)###1055");
            Assert.That(Window.DebugBeginCount, Is.EqualTo(1));
            Window.DebugLeaveEnd();
            Assert.That(Window.DebugBeginCount, Is.EqualTo(0));
        }

        [Test]
        public void EndWithoutBegin_Throws()
        {
            var ex = Assert.Throws<InvalidOperationException>(Window.DebugLeaveEnd);
            Assert.That(ex.Message, Does.Contain("without a matching Begin"));
        }

        [Test]
        public void Scope_EndsWhenBeginReturnsFalse()
        {
            Window.DebugSuppressImGui = true;
            Window.DebugBeginVisible = false;
            bool open = true;

            using (var w = Window.DebugScope("GameLog", ref open))
            {
                Assert.That(w.Visible, Is.False);
                Assert.That(Window.DebugBeginCount, Is.EqualTo(1));
            }

            Assert.That(Window.DebugBeginCount, Is.EqualTo(0));
        }

        [Test]
        public void Scope_EndsWhenBodyReturnsEarly()
        {
            Window.DebugSuppressImGui = true;
            Window.DebugBeginVisible = true;
            bool open = true;

            DrawLikeGameLog();
            Assert.That(Window.DebugBeginCount, Is.EqualTo(0));

            void DrawLikeGameLog()
            {
                using var w = Window.DebugScope("GameLog", ref open);
                if (!w.Visible)
                    return;
                // Empty log used to return here without End; the scope still Ends.
                return;
            }
        }

        [Test]
        public void Scope_EndsWhenBodyThrows()
        {
            Window.DebugSuppressImGui = true;
            Window.DebugBeginVisible = true;
            bool open = true;

            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                using var w = Window.DebugScope("GameLog", ref open);
                throw new InvalidOperationException("body failed");
            });
            Assert.That(ex.Message, Is.EqualTo("body failed"));
            Assert.That(Window.DebugBeginCount, Is.EqualTo(0));
        }

        [Test]
        public void Scope_AllowsTheNextWindowAfterACollapsedBegin()
        {
            Window.DebugSuppressImGui = true;
            Window.DebugBeginVisible = false;
            bool open = true;

            using (var w = Window.DebugScope("GameLog", ref open))
            {
                Assert.That(w.Visible, Is.False);
            }

            Window.DebugBeginVisible = true;
            using (var w = Window.DebugScope("Unknown (Unknown)###1055", ref open))
            {
                Assert.That(w.Visible, Is.True);
                Assert.That(Window.DebugBeginCount, Is.EqualTo(1));
            }

            Assert.That(Window.DebugBeginCount, Is.EqualTo(0));
        }
    }
}
