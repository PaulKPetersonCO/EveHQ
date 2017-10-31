// ==============================================================================
// 
// EveHQ - An Eve-Online™ character assistance application
// Copyright © 2005-2015  EveHQ Development Team
//   
// This file is part of EveHQ.
//  
// The source code for EveHQ is free and you may redistribute 
// it and/or modify it under the terms of the MIT License. 
// 
// Refer to the NOTICES file in the root folder of EVEHQ source
// project for details of 3rd party components that are covered
// under their own, separate licenses.
// 
// EveHQ is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the MIT 
// license below for details.
// 
// ------------------------------------------------------------------------------
// 
// The MIT License (MIT)
// 
// Copyright © 2005-2015  EveHQ Development Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------
// 
// <copyright file="TaskExtensions.cs" company="EveHQ Development Team">
//     Copyright © 2005-2015  EveHQ Development Team
// </copyright>
// 
// ==============================================================================

#region Usings

using System;
using System.Diagnostics;
using System.Threading.Tasks;

#endregion


namespace EveHQ.Common.Extensions
{
	public static class TaskExtensions
	{
		/// <summary>Attempts to run the task, and captures any failures to the trace log</summary>
		/// <param name="factory">The factory.</param>
		/// <param name="action">The action.</param>
		/// <returns>The <see cref="Task" />.</returns>
		public static Task TryRun(this TaskFactory factory, Action action)
		{
			if (factory != null)
			{
				return factory.StartNew(action).ContinueWith(t => VerifyTaskCompletedSuccessFully(t));
			}

			return null;
		}

		/// <summary>Attempts to run the task, and captures any failures to the trace log</summary>
		/// <param name="factory">The factory.</param>
		/// <param name="action">The action.</param>
		/// <typeparam name="T">The result type of the task</typeparam>
		/// <returns>The <see cref="Task" />.</returns>
		public static Task<T> TryRun<T>(this TaskFactory<T> factory, Func<T> action)
		{
			if (factory != null)
			{
				return factory.StartNew(action).ContinueWith(t => VerifyTaskCompletedSuccessFully(t));
			}

			return null;
		}

		public static bool IsTaskSuccessfullyCompleted<T>(this Task<T> task) where T: class => 
			task.IsCompleted && task.Result != null && !task.IsCanceled && !task.IsFaulted && task.Exception == null;

		/// <summary>The verify task completed success fully.</summary>
		/// <param name="task">The task.</param>
		/// <returns>The <see cref="Task" />.</returns>
		private static Task VerifyTaskCompletedSuccessFully(Task task)
		{
			if (task.IsFaulted &&
				task.Exception != null)
			{
				Trace.TraceError(task.Exception.FormatException());
				Trace.Flush();
			}

			return task;
		}

		/// <summary>The verify task completed success fully.</summary>
		/// <param name="task">The task.</param>
		/// <typeparam name="T">the type of task result</typeparam>
		/// <returns>The <see cref="T" />.</returns>
		private static T VerifyTaskCompletedSuccessFully<T>(Task<T> task)
		{
			if (task.IsFaulted &&
				task.Exception != null)
			{
				Trace.TraceError(task.Exception.FormatException());
				Trace.Flush();
				return default(T);
			}

			return task.Result;
		}
	}
}