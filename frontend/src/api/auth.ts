// <copyright file="auth.ts" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import type { Company, FiscalPeriod, User } from '@/types'
import { get, post } from '@/api/client'

export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  accessToken: string
  refreshToken: string
  isSuperAdmin: boolean
  user: User
  companies: Company[]
  fiscalPeriods: FiscalPeriod[]
}

export function loginUser(data: LoginRequest): Promise<LoginResponse> {
  return post<LoginResponse>('/auth/login', data)
}

export function getMe(): Promise<User> {
  return get<User>('/auth/me')
}
