import { Link } from 'react-router-dom'
import { Home, RotateCcw, Search, HelpCircle } from 'lucide-react'
import { Button } from '@components/ui/Button'
import { Card, CardContent } from '@components/ui/Card'

export function NotFoundPage() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900 px-4">
      <div className="max-w-md w-full text-center">
        <div className="mb-8">
          <div className="inline-flex items-center justify-center w-24 h-24 rounded-full bg-primary-100 dark:bg-primary-900/30 mb-6">
            <Search className="h-12 w-12 text-primary-600 dark:text-primary-400" aria-hidden="true" />
          </div>
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-2">404</h1>
          <p className="text-xl text-gray-600 dark:text-gray-400">Page Not Found</p>
        </div>

        <Card variant="bordered">
          <CardContent className="p-8 space-y-6">
            <div className="space-y-2">
              <p className="text-lg text-gray-900 dark:text-white">
                Sorry, we couldn't find the page you're looking for.
              </p>
              <p className="text-gray-500 dark:text-gray-400">
                The page might have been moved, deleted, or the URL might be incorrect.
              </p>
            </div>

            <div className="space-y-3 pt-4 border-t border-gray-200 dark:border-gray-700">
              <Button size="lg" className="w-full" asChild>
                <Link to="/dashboard">
                  <Home className="h-4 w-4 mr-2" />
                  Go to Dashboard
                </Link>
              </Button>

              <Button variant="outline" size="lg" className="w-full" asChild>
                <Link to={window.location.pathname}>
                  <RotateCcw className="h-4 w-4 mr-2" />
                  Refresh Page
                </Link>
              </Button>

              <Button variant="ghost" size="lg" className="w-full" asChild>
                <Link to="/reporting">
                  <HelpCircle className="h-4 w-4 mr-2" />
                  Browse Reports
                </Link>
              </Button>
            </div>

            <div className="pt-4 border-t border-gray-200 dark:border-gray-700">
              <p className="text-sm text-gray-500 dark:text-gray-400">
                Still need help?{' '}
                <a href="/support" className="text-primary-600 dark:text-primary-400 hover:underline font-medium">
                  Contact Support
                </a>
              </p>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}