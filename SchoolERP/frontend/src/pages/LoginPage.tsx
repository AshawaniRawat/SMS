import { Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useState } from 'react';
const schema = z.object({ email: z.email('Enter a valid email'), password: z.string().min(1, 'Enter your password') });
export function LoginPage() {
  const [submitted, setSubmitted] = useState(false);
  const { register, handleSubmit, formState: { errors } } = useForm<z.infer<typeof schema>>({ resolver: zodResolver(schema) });
  return <div className="grid min-h-screen place-items-center bg-background p-5"><section className="w-full max-w-md rounded-xl border border-border bg-card p-8"><p className="text-sm font-semibold text-primary">SchoolERP</p><h1 className="mt-4 text-2xl font-semibold">Welcome to your workspace</h1><p className="mt-2 text-sm text-muted-foreground">Login preview. Authentication is coming in a later phase.</p><form className="mt-6 space-y-4" onSubmit={handleSubmit(() => setSubmitted(true))}><label className="block text-sm">Email<input {...register('email')} type="email" autoComplete="username" className="mt-2 w-full rounded-lg border border-border bg-background p-3"/>{errors.email && <span className="text-xs text-red-600">{errors.email.message}</span>}</label><label className="block text-sm">Password<input {...register('password')} type="password" autoComplete="current-password" className="mt-2 w-full rounded-lg border border-border bg-background p-3"/>{errors.password && <span className="text-xs text-red-600">{errors.password.message}</span>}</label><button className="w-full rounded-lg bg-primary p-3 text-sm font-semibold text-primary-foreground">Preview sign in</button>{submitted && <p role="status" className="text-sm text-muted-foreground">This is a placeholder. No credentials were sent or stored.</p>}</form><Link to="/" className="mt-5 inline-block text-sm text-primary">Back to demo dashboard →</Link></section></div>;
}