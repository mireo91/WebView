﻿using System;
using NUnit.Framework;

namespace Tests {

    public class CommonTests : WebViewTestBase {

        [Test(Description = "Attached listeners are called")]
        public void ListenersAreCalled() {
            var listener1Counter = 0;
            var listener2Counter = 0;

            var listener1 = TargetView.AttachListener("event1_name");
            listener1.Handler += () => listener1Counter++;

            var listener2 = TargetView.AttachListener("event2_name");
            listener2.Handler += () => listener2Counter++;
            listener2.Handler += () => listener2Counter++;

            LoadAndWaitReady($"<html><script>{listener1}{listener2}</script><body></body></html>");
            WaitFor(() => listener1Counter > 0 && listener2Counter > 0);
            Assert.AreEqual(1, listener1Counter);
            Assert.AreEqual(2, listener2Counter);
        }

        [Test(Description = "Attached listeners are called in Dispatcher thread")]
        public void ListenersAreCalledInDispatcherThread() {
            bool? canAccessDispatcher = null;
            var listener = TargetView.AttachListener("event_name");
            listener.UIHandler += () => canAccessDispatcher = TargetView.Dispatcher.CheckAccess();
            LoadAndWaitReady($"<html><script>{listener}</script><body></body></html>");
            WaitFor(() => canAccessDispatcher != null);
            Assert.IsTrue(canAccessDispatcher);
        }

        [Test(Description = "Unhandled Exception event is called when an async unhandled error occurs inside a listener")]
        public void UnhandledExceptionEventIsCalledOnListenerError() {
            const string ExceptionMessage = "hey";

            Exception exception = null;

            WithUnhandledExceptionHandling(() => {
                var listener = TargetView.AttachListener("event_name");
                listener.Handler += () => throw new Exception(ExceptionMessage);

                LoadAndWaitReady($"<html><script>{listener}</script><body></body></html>");

                WaitFor(() => exception != null);
                Assert.IsTrue(exception.Message.Contains(ExceptionMessage));
            }, 
            e => {
                exception = e;
                return true;
            });
        }

        [Test(Description = "Before navigate hook is called")]
        public void BeforeNavigateHookCalled() {
            var beforeNavigatedCalled = false;
            TargetView.BeforeNavigate += (request) => {
                request.Cancel();
                beforeNavigatedCalled = true;
            };
            TargetView.Address = "https://www.google.com";
            WaitFor(() => beforeNavigatedCalled);
            Assert.IsTrue(beforeNavigatedCalled);
        }

        [Test(Description = "Javascript evaluation on navigated event does not block")]
        public void JavascriptEvaluationOnNavigatedDoesNotBlock() {
            var navigated = false;
            TargetView.Navigated += _ => {
                TargetView.EvaluateScript<int>("1+1");
                navigated = true;
            };
            LoadAndWaitReady("<html>><body></body></html>");

            WaitFor(() => navigated);
            Assert.IsTrue(navigated);
        }

    }
}
